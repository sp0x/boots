using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.Batching;
using nvoid.db.Caching;
using nvoid.db.DB;
using nvoid.db.Extensions;
using nvoid.extensions;
using nvoid.Integration;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.IntegrationSource;
using Netlyt.Service.Lex.Generation;
using Netlyt.Service.Models;
using Netlyt.Service.Time;
using Netlyt.ServiceTests.IntegrationSource;
using Xunit;

namespace Netlyt.ServiceTests.Lex
{
    [Collection("Entity Parsers")]
    public class DonutTests
    {
        private ConfigurationFixture _config;
        private CrossSiteAnalyticsHelper _helper;
        private IMongoCollection<IntegratedDocument> _documentStore;
        private DateHelper _dateHelper;
        private DynamicContextFactory _contextFactory;
        private ManagementDbContext _context;
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private ApiAuth _appId;
        private RedisCacher _cacher;
        public const byte VisitTypedValue = 1;

        public DonutTests(ConfigurationFixture fixture)
        {
            _config = fixture;
            _helper = new CrossSiteAnalyticsHelper();
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>().AsMongoDbQueryable();
            _dateHelper = new DateHelper();
            _contextFactory = new DynamicContextFactory(() => _config.CreateContext());
            _context = _contextFactory.Create();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _appId = _apiService.Generate();
            _apiService.Register(_appId);
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
            var harvester = new Netlyt.Service.Harvester<IntegratedDocument>(_apiService, _integrationService, 10);
            var type = harvester.AddIntegrationSource("NetInfoUserFeatures_7_8_1", _appId.AppId, source);
            var donutMachine = new DonutfileGenerator<NetinfoDonutfile, NetinfoDonutContext>(type, _cacher); 
            var donut = donutMachine.Generate();

            var dictEval = new MemberVisitingBlock(donut.ProcessRecord);
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
                doc.IntegrationId = type.Id; doc.APIId = _appId.Id;
                x.Features = null;
                return doc;
            });
            var insertBatcher = new MongoInsertBatch<IntegratedDocument>(_documentStore, 3000);

            demographyImporter.Helper = _helper;
            //dictEval.LinkOnComplete(demographyImporter); // retoggle
            demographyImporter.LinkTo(featureGeneratorBlock);
            featureGeneratorBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            insertCreator.LinkTo(insertBatcher.BatchBlock, new DataflowLinkOptions { PropagateCompletion = true });

            //dictEval.AddFlowCompletionTask(insertBatcher.Completion); //retoggle
            //harvester
            //->unpack reduced events
            //->pass each event through AccumulateUserDocument to collect stats
            //->bing in demographic data to the already grouped userbase
            //->pass the day_user document through FeatureGenerator to create it's features
            harvester.SetDestination(dictEval);
            var result = await harvester.Synchronize();
            Debug.WriteLine(result.ProcessedEntries);
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

        private object AccumulateUserDocument(IntegratedDocument accumulator, BsonDocument newEntry, bool appendEvent = true)
        {
            return newEntry;
//            var value = newEntry.GetString("value");
//            var onDate = newEntry.GetDate("ondate").Value;
//            var uuid = accumulator.GetString("uuid");
//            //CollectTypeValuePair(uuid, newEntry);
//            //            var newElement = new
//            //            {
//            //                ondate = newEntry.GetDate("ondate"),
//            //                event_id = newEntry.GetInt("event_id"),
//            //                type = newEntry.GetInt("type"),
//            //                value = newEntry.GetString("value")
//            //            }.ToBsonDocument();
//            var pageHost = value.ToHostname();
//            var pageSelector = pageHost;
//            var isNewPage = false;
//            if (!_helper.Stats.ContainsPage(pageHost))
//            {
//                _helper.Stats.AddPage(pageSelector, new PageStats()
//                {
//                    Page = value
//                });
//            }
//            _helper.Stats[pageSelector].PageVisitsTotal++;
//
//            if (appendEvent)
//            {
//                //accumulator.AddDocumentArrayItem("events", newEntry);
//            }
//            if (value.Contains("payments/finish") && value.ToHostname().Contains("ebag.bg"))
//            {
//                if (DateHelper.IsHoliday(onDate))
//                {
//                    _helper.PurchasesOnHolidays.Add(newEntry);
//                }
//                else if (DateHelper.IsHoliday(onDate.AddDays(1)))
//                {
//                    _helper.PurchasesBeforeHolidays.Add(newEntry);
//                }
//                else if (onDate.DayOfWeek == DayOfWeek.Friday)
//                {
//                    _helper.PurchasesBeforeWeekends.Add(newEntry);
//                }
//                else if (onDate.DayOfWeek > DayOfWeek.Friday)
//                {
//                    _helper.PurchasesInWeekends.Add(newEntry);
//                }
//                _helper.Purchases.Add(newEntry);
//                accumulator["is_paying"] = 1;
//            }
//            return newEntry;
        }

    }
}
