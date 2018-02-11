﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.Batching;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.db.Extensions;
using nvoid.Integration;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.Integration.Import;
using Netlyt.Service.IntegrationSource;
using Netlyt.Service.Lex.Generation;
using Netlyt.Service.Models;
using Netlyt.Service.Time;
using Xunit;

namespace Netlyt.ServiceTests.Lex
{
    [Collection("DonutTests")]
    public class DonutTests
    {
        private DonutConfigurationFixture _config;
        private CrossSiteAnalyticsHelper _helper;
        private IMongoCollection<IntegratedDocument> _documentStore;
        private DateHelper _dateHelper;
        private DynamicContextFactory _contextFactory;
        private ManagementDbContext _context;
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private ApiAuth _appAuth;
        private RedisCacher _cacher;
        public const byte VisitTypedValue = 1;

        public DonutTests(DonutConfigurationFixture fixture)
        {
            _config = fixture;
            _helper = new CrossSiteAnalyticsHelper();
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>().AsMongoDbQueryable();
            _dateHelper = new DateHelper();
            _contextFactory = new DynamicContextFactory(() => _config.CreateContext());
            _context = _contextFactory.Create();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _appAuth = _apiService.Generate();
            _apiService.Register(_appAuth);
            _cacher = fixture.GetService<RedisCacher>();
        }

        [Theory]
        [InlineData(new object[] { "057cecc6-0c1b-44cd-adaa-e1089f10cae8_reduced", "TestData\\Ebag\\demograpy.csv" })]
        public async void ExtractEntitiesFromReducedCollection(string collectionName, string demographySheet)
        {
            MongoSource source = MongoSource.CreateFromCollection(collectionName, new BsonFormatter());

            source.SetProjection(x =>
            {
                if (!x["value"].AsBsonDocument.Contains("day")) x["value"]["day"] = x["_id"]["day"];
                return x["value"] as BsonDocument;
            });
            source.ProgressInterval = 0.05;
            var harvester = new Netlyt.Service.Harvester<IntegratedDocument>(_apiService, _integrationService, 10);
            harvester.LimitEntries(10000);
            var type = harvester.AddIntegrationSource("NetInfoUserFeatures_7_8_1", _appAuth.AppId, source);
            //hehe
            var donutMachine = new DonutfileGenerator<NetinfoDonutfile, NetinfoDonutContext>(type, _cacher);
            var donut = donutMachine.Generate();
            donut.SetupCacheInterval(source.Size);

            var metaBlock = new MemberVisitingBlock(donut.ProcessRecord);
            _helper = new CrossSiteAnalyticsHelper();

            var featureHelper = new FeatureGeneratorHelper() { Helper = _helper, TargetDomain = "ebag.bg" };
            var featureGenerator = new FeatureGenerator<IntegratedDocument>(
                new Func<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>[]
            {
                featureHelper.GetFeatures,
                featureHelper.GetAvgTimeBetweenSessionFeatures
            }, 16);
            var featureGeneratorBlock = featureGenerator.CreateFeaturesBlock();

            var demographyImporter = new EntityDataImporter(demographySheet, true);
            demographyImporter.UseInputKey((input) => input[0]);
            demographyImporter.SetEntityKey((input) => input.GetString("uuid"));
            demographyImporter.JoinOn(JoinDemography);
            demographyImporter.ReadData();

            var insertCreator = new TransformBlock<FeaturesWrapper<IntegratedDocument>, IntegratedDocument>((x) =>
            {
                var doc = x.Document;
                //Cleanup
                doc.Document.Value.Remove("events");
                doc.Document.Value.Remove("browsing_statistics");
                foreach (var featurePair in x.Features)
                {
                    var name = featurePair.Key;
                    if (string.IsNullOrEmpty(name)) continue;
                    var featureval = featurePair.Value;
                    doc.Document.Value.Set(name, BsonValue.Create(featureval));
                }
                //Cleanup
                doc.IntegrationId = type.Id; doc.APIId = _appAuth.Id;
                x.Features = null;
                return doc;
            });
            var insertBatcher = new MongoInsertBatch<IntegratedDocument>(_documentStore, 3000);

            demographyImporter.Helper = _helper;
            metaBlock.LinkOnComplete(demographyImporter); // retoggle
            demographyImporter.LinkTo(featureGeneratorBlock);
            featureGeneratorBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            insertCreator.LinkTo(insertBatcher.BatchBlock, new DataflowLinkOptions { PropagateCompletion = true });

            metaBlock.AddFlowCompletionTask(insertBatcher.Completion); //retoggle
            //harvester
            //->unpack reduced events
            //->pass each event through AccumulateUserDocument to collect stats
            //->bing in demographic data to the already grouped userbase
            //->pass the day_user document through FeatureGenerator to create it's features
            harvester.SetDestination(metaBlock);
            var result = await harvester.Run();
            Debug.WriteLine(result.ProcessedEntries);
        }

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\demograpy.csv" })]
        public async void TestDataSourceInput(string inputFile)
        {
            var currentDir = Environment.CurrentDirectory;
            inputFile = Path.Combine(currentDir, inputFile);
            Console.WriteLine($"Parsing data in: {inputFile}");
            uint entryLimit = 0;
            var importTask = new DataImportTask<ExpandoObject>(_apiService, _integrationService, new DataImportTaskOptions
            {
                Source = FileSource.CreateFromFile(inputFile, new CsvFormatter()
                {
                    Delimiter = ',',
                    Headers = new[] { "uuid", "time", "gender", "age" },
                    SkipHeader = true
                }),
                ApiKey = _appAuth,
                TypeName = "TestingType",
                ThreadCount = 1, //So that we actually get predictable results with our limit!
                TotalEntryLimit = entryLimit
            }.AddIndex("uuid"));
            var importResult = await importTask.Import();
            if (entryLimit > 0)
            {
                Assert.True(entryLimit == importResult.Data.ProcessedEntries);
            }
            else
            {
                Assert.True(importResult.Data.ProcessedEntries>0);
            } 
            importResult.Collection.Trash();
        }

        [Theory]
        [InlineData(new object[]
       {
            "TestData\\Ebag\\NewJoin",
           @"reduce day = time(this.ondate) / (60*60*24), 
                uuid = this.uuid
                reduce_map  ondate = this.ondate,
                value = this.value,
                type = this.type
            reduce aggregate
                events = selectMany(values, (x) => x.events),
                uuid = key.uuid,
                day = key.day,
                noticed_date = if(any(events), events[0].ondate, null)"
       })]
        public async void TestMapReduceDonut(string inputDirectory, string mapReduceDonut)
        {
            var currentDir = Environment.CurrentDirectory;
            inputDirectory = Path.Combine(currentDir, inputDirectory);
            Console.WriteLine($"Parsing data in: {inputDirectory}");
            uint entryLimit = 100;
            var importTask = new DataImportTask<ExpandoObject>(_apiService, _integrationService, new DataImportTaskOptions
            {
                Source = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter() { Delimiter = ';' }),
                ApiKey = _appAuth,
                TypeName = "TestingType",
                ThreadCount = 1, //So that we actually get predictable results with our limit!
                TotalEntryLimit = entryLimit
            }.AddIndex("ondate"));
            var importResult = await importTask.Import();
            await importTask.Reduce(mapReduceDonut, entryLimit, Builders<BsonDocument>.Sort.Ascending("ondate"));
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var reducedCollection = new MongoList(databaseConfiguration, importTask.OutputCollection.ReducedOutputCollection);
            var reducedDocsCount = reducedCollection.Size;
            Assert.Equal(64, reducedDocsCount);
            //Cleanup
            importResult.Collection.Trash();
            reducedCollection.Trash();
        }


        private void JoinDemography(string[] demographyFields, IntegratedDocument userDocument)
        {
            int tAge;
            var userDocumentDocument = userDocument.GetDocument();
            if (int.TryParse(demographyFields[3], out tAge)) userDocumentDocument["age"] = tAge;
            var gender = demographyFields[2];
            if (gender.Length > 0)
            {
                int genderId = 0;
                if (gender == "male") genderId = 1;
                else if (gender == "female") genderId = 2;
                else if (gender == "t") genderId = 1;
                else if (gender == "f") genderId = 2;
                userDocumentDocument["gender"] = genderId; //Gender is t or f anyways
            }
            else
            {
                userDocumentDocument["gender"] = 0;
            }
        }

    }
}
