using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.MongoDB;
using nvoid.db.Extensions;
using nvoid.extensions;
using Peeralize.Service;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Time;
using Peeralize.ServiceTests.IntegrationSource;
using Xunit;

namespace Peeralize.ServiceTests.Netinfo
{
    [Collection("Entity Parsers")]
    public class NetinfoTests
    {
        public const byte VisitTypedValue = 1;
        private ConfigurationFixture _config;
        private CrossSiteAnalyticsHelper helper;
        private IMongoCollection<IntegratedDocument> _documentStore;
        private string _appId;
        private BsonArray _purchases;
        private BsonArray _purchasesOnHolidays;
        private BsonArray _purchasesBeforeHolidays;
        private BsonArray _purchasesInWeekends;
        private BsonArray _purchasesBeforeWeekends;
        private DateHelper _dateHelper;


        public NetinfoTests(ConfigurationFixture fixture)
        {
            _config = fixture;
            helper = new CrossSiteAnalyticsHelper();
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>().MongoDb();
            _appId = "123123123";
        }
        /// <summary>
        /// TODO: Make grouping keys(day) be day since unix timestamp start
        /// </summary>
        /// <param name="inputDirectory"></param>
        /// <param name="targetDomain"></param>
        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156", "ebag.bg" })] 
        public async void ExtractAvgTimeBetweenVisitFeatures(string inputDirectory, string targetDomain)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter() { Delimiter = ';' });
            var harvester = new Peeralize.Service.Harvester(10);
            harvester.AddPersistentType(fileSource, _appId, true);

            var grouper = new GroupingBlock(_appId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.Day}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                AccumulateUserDocument);
            grouper.Helper = helper;
            //Group the users
            // create features for each user -> create Update -> batch update
            var featureHelper = new FeatureGeneratorHelper() { Helper = helper, TargetDomain = "ebag.bg"};
            var featureGenerator = new FeatureGenerator(featureHelper.GetAvgTimeBetweenSessionFeatures, 8);
            var updateCreator = new TransformBlock<DocumentFeatures, FindAndModifyArgs<IntegratedDocument>>((docFeatures) =>
            {
                return new FindAndModifyArgs<IntegratedDocument>()
                {
                    Query = Builders<IntegratedDocument>.Filter.And(
                                Builders<IntegratedDocument>.Filter.Eq("Document.uuid", docFeatures.Document["uuid"].ToString()),
                                Builders<IntegratedDocument>.Filter.Eq("Document.noticed_date", docFeatures.Document.GetDate("noticed_date"))),
                    Update = docFeatures.Features.ToMongoUpdate<IntegratedDocument, object>()
                };
            });
            var updateBatcher = new MongoUpdateBatch<IntegratedDocument>(_documentStore, 300);
            var featuresBlock = featureGenerator.CreateFeaturesBlock();
            featuresBlock.LinkTo(updateCreator);
            updateCreator.LinkTo(updateBatcher.Block);
            grouper.AddFlowCompletionTask(featuresBlock.Completion);
            grouper.AddFlowCompletionTask(updateBatcher.Block.Completion);
            grouper.LinkOnComplete(new TransformBlock<IntegratedDocument, IntegratedDocument>(doc =>
            {
                featuresBlock.Post(doc);
                return doc;
            }));

            harvester.SetDestination(grouper);
            var completion = await harvester.Synchronize();
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
        }


        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\JoinedData", "TestData\\Ebag\\demograpy.csv" })]
        public async void ExtractEntityFromDirectory(string inputDirectory, string demographySheet)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter(){ Delimiter = ';' });

            var userId = "123123123"; 
            var harvester = new Peeralize.Service.Harvester();
            var type = harvester.AddPersistentType(fileSource, userId, true);
            
            var grouper = new GroupingBlock(_appId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.DaysTotal()}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                AccumulateUserDocument);

            var helper = new CrossSiteAnalyticsHelper(grouper.EntityDictionary, grouper.PageStats);
            var featureHelper = new FeatureGeneratorHelper() { Helper = helper, TargetDomain = "ebag.bg" };
            var featureGenerator = new FeatureGenerator(featureHelper.GetFeatures, 8);
            featureGenerator.AddGenerator(featureHelper.GetAvgTimeBetweenSessionFeatures);
            var featureGeneratorBlock = featureGenerator.CreateFeaturesBlock();

            var demographyImporter = new EntityDataImporter(
                demographySheet, true);
            //demographyImporter.SetEntityRelation((input, x) => input[0] == x.Document["uuid"]);
            demographyImporter.SetDataKey((input) => input[0]);
            demographyImporter.SetEntityKey((IntegratedDocument input) => input.GetString("uuid"));
            demographyImporter.JoinOn(JoinDemography);
            demographyImporter.Map();

            var insertCreator = new TransformBlock<DocumentFeatures, IntegratedDocument>((x) =>
            { 
                var doc = x.Document;
                //Todo: Fill doc with features 
                return doc;
            });
            var insertBatcher = new MongoInsertBatch<IntegratedDocument>(_documentStore, 3000);
            
            demographyImporter.Helper = helper;
            grouper.Helper = helper;
             
            demographyImporter.LinkTo(featureGeneratorBlock);
            featureGeneratorBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            insertCreator.LinkTo(insertBatcher.Block);

            grouper.LinkOnComplete(demographyImporter);
            grouper.AddFlowCompletionTask(insertBatcher.Completion);

            harvester.SetDestination(grouper);
            harvester.AddType(type, fileSource);
            var result = await harvester.Synchronize();
            Console.ReadLine();
        }



        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void ExtractEventValueFeatures(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter() { Delimiter = ';' });
            var harvester = new Peeralize.Service.Harvester(10);
            harvester.AddPersistentType(fileSource, _appId, true);

            var grouper = new GroupingBlock(_appId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.Day}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                (accumulator, newEntry) =>
                {
                    var newElement = new
                    {
                        ondate = newEntry.GetDate("ondate"),
                        event_id = newEntry.GetInt("event_id"),
                        type = newEntry.GetInt("type"),
                        value = newEntry.GetString("value")
                    }.ToBsonDocument();
                    accumulator.AddDocumentArrayItem("events", newElement);
                    return newElement;
                });
            grouper.Helper = helper;
            grouper.LinkTo(new ActionBlock<IntegratedDocument>((x) =>
            {
                CollectTypeValuePair(x);
            }));
            //Group the users
            // create features for each user -> create Update -> batch update
            var featureGenerator = new FeatureGenerator((doc) => 
                helper.GetTopRatedFeatures(doc["uuid"].ToString(), VisitTypedValue, 10)
                .Select((value, index) => new KeyValuePair<string, object>($"Document._has_type_val_{index}", value))
            );
            var updateCreator = new TransformBlock<DocumentFeatures, FindAndModifyArgs<IntegratedDocument>>((x) =>
            {
                return new FindAndModifyArgs<IntegratedDocument>()
                {
                    Query = Builders<IntegratedDocument>.Filter.And(
                                Builders<IntegratedDocument>.Filter.Eq("Document.uuid", x.Document["uuid"].ToString()),
                                Builders<IntegratedDocument>.Filter.Eq("Document.noticed_date", x.Document.GetDate("noticed_date"))),
                    Update = x.Features.ToMongoUpdate<IntegratedDocument, object>()
                };
            });
            var updateBatcher = new MongoUpdateBatch<IntegratedDocument>(_documentStore, 300);
            var featuresBlock = featureGenerator.CreateFeaturesBlock();
            featuresBlock.LinkTo(updateCreator);
            updateCreator.LinkTo(updateBatcher.Block); 
            
            grouper.LinkOnCompleteEx(featuresBlock); 

            harvester.SetDestination(grouper); 
            var completion = await harvester.Synchronize();
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");

        }

        /// <summary>
        /// Adds the type&value pair combinations in the helper
        /// </summary>
        /// <param name="block"></param>
        /// <param name="arg"></param>
        private void CollectTypeValuePair(IntegratedDocument arg)
        {
            var value = arg.GetString("value");
            int intval;
            if (int.TryParse(value, out intval))
            {
                var ev = int.Parse(arg["type"].ToString());
                var key_value = $"{ev}_{intval}";
                var uuid = arg["uuid"]?.ToString();
                if (string.IsNullOrEmpty(uuid)) return;
                helper.AddRatingFeature(VisitTypedValue, uuid, key_value);
            }

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

        private object AccumulateUserDocument(IntegratedDocument accumulator, IntegratedDocument newEntry)
        {
            var value = newEntry.GetString("value");
            var onDate = newEntry.GetDate("ondate").Value;
            CollectTypeValuePair(newEntry);
            var newElement = new
            {
                ondate = newEntry.GetDate("ondate"),
                event_id = newEntry.GetInt("event_id"),
                type = newEntry.GetInt("type"),
                value = newEntry.GetString("value")
            }.ToBsonDocument();
            accumulator.AddDocumentArrayItem("events", newElement);
            if (value.Contains("payments/finish") && value.ToHostname().Contains("ebag.bg"))
            {
                if (_dateHelper.IsHoliday(onDate))
                {
                    helper.PurchasesOnHolidays.Add(newElement);
                }
                else if (_dateHelper.IsHoliday(onDate.AddDays(1)))
                {
                    helper.PurchasesBeforeHolidays.Add(newElement);
                }
                else if (onDate.DayOfWeek == DayOfWeek.Friday)
                {
                    helper.PurchasesBeforeWeekends.Add(newElement);
                }
                else if (onDate.DayOfWeek > DayOfWeek.Friday)
                {
                    helper.PurchasesInWeekends.Add(newElement);
                }
                helper.Purchases.Add(newElement);
                accumulator["is_paying"] = 1; 
            }
            return newElement;
        } 

    }
}
