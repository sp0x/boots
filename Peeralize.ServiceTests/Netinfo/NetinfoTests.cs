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
using Peeralize.Service;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource; 
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
                AccumulateUserEvent);
            grouper.Helper = helper;
            //Group the users
            // create features for each user -> create Update -> batch update
            var featureGenerator = new FeatureGenerator((doc) =>
                {
                    int min = 0, max = 604800;
                    var sessions = CrossSiteAnalyticsHelper.GetWebSessions(doc, targetDomain)
                        .Select(x=>x.Visited).ToList();
                    var timeBetweenSessionSum = 0.0d;
                    for(var i=0; i<sessions.Count; i++)
                    {
                        if (i == 0) continue;
                        var session = sessions[i];
                        var diff = session - sessions[i - 1];
                        timeBetweenSessionSum += diff.TotalSeconds;
                    }
                    timeBetweenSessionSum = timeBetweenSessionSum / Math.Max(sessions.Count, 1);
                    if (timeBetweenSessionSum > 0)
                    {
                        timeBetweenSessionSum = timeBetweenSessionSum;
                    }
                    timeBetweenSessionSum = timeBetweenSessionSum == 0 ? 0 : (1 - (timeBetweenSessionSum / max));
                    return new []{
                        new KeyValuePair<string, double>("Document.time_between_visits_avg", timeBetweenSessionSum)
                    };
                }
            , 8);
            var updateCreator = new TransformBlock<DocumentFeatures, FindAndModifyArgs<IntegratedDocument>>((docFeatures) =>
            {
                return new FindAndModifyArgs<IntegratedDocument>()
                {
                    Query = Builders<IntegratedDocument>.Filter.And(
                                Builders<IntegratedDocument>.Filter.Eq("Document.uuid", docFeatures.Document["uuid"].ToString()),
                                Builders<IntegratedDocument>.Filter.Eq("Document.noticed_date", docFeatures.Document.GetDate("noticed_date"))),
                    Update = docFeatures.Features.ToMongoUpdate<IntegratedDocument, double>()
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
                AccumulateUserEvent);
            //var saver = new MongoSink(userId); 
            //grouper.LinkTo(DataflowBlock.NullTarget<IntegratedDocument>());
            grouper.Helper = helper;
            //demographyImporter.LinkTo(featureGen); 
            //featureGen.LinkTo(saver);
            grouper.LinkTo(new ActionBlock<IntegratedDocument>((x) =>
            {
                CollectTypedValues(grouper, x);
            }));
            //Group the users
            // create features for each user -> create Update -> batch update
            var featureGenerator = new FeatureGenerator((doc) => 
                helper.GetTopRatedFeatures(doc["uuid"].ToString(), VisitTypedValue, 10)
                .Select((value, index) => new KeyValuePair<string, double>($"Document._has_type_val_{index}", value))
            );
            var updateCreator = new TransformBlock<DocumentFeatures, FindAndModifyArgs<IntegratedDocument>>((x) =>
            {
                return new FindAndModifyArgs<IntegratedDocument>()
                {
                    Query = Builders<IntegratedDocument>.Filter.And(
                                Builders<IntegratedDocument>.Filter.Eq("Document.uuid", x.Document["uuid"].ToString()),
                                Builders<IntegratedDocument>.Filter.Eq("Document.noticed_date", x.Document.GetDate("noticed_date"))),
                    Update = x.Features.ToMongoUpdate<IntegratedDocument, double>()
                };
            });
            var updateBatcher = new MongoUpdateBatch<IntegratedDocument>(_documentStore, 300);
            var featuresBlock = featureGenerator.CreateFeaturesBlock();
            featuresBlock.LinkTo(updateCreator);
            updateCreator.LinkTo(updateBatcher.Block); 
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

        private void UpdateFeatures(string appId, FilterDefinition<IntegratedDocument> query, UpdateDefinition<IntegratedDocument> newUpdate)
        {
            query = Builders<IntegratedDocument>.Filter.And(Builders<IntegratedDocument>.Filter.Eq("UserId", appId), query);
            var args = new FindAndModifyArgs<IntegratedDocument>()
            {
                Query = query,
                Update = newUpdate
            };
            var ret = _documentStore.UpdateOne(args.Query, args.Update);
        }

        private void CollectTypedValues(IntegrationBlock block, IntegratedDocument arg)
        {
            var value = arg["value"].ToString();
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

        /// <summary>
        /// Format the input element that should be added
        /// </summary>
        /// <param name="accumulator"></param>
        /// <param name="newEntry"></param>
        private object AccumulateUserEvent(IntegratedDocument accumulator, IntegratedDocument newEntry)
        {
            var value = newEntry["value"]?.ToString();
            var onDate = newEntry["ondate"].ToString();
            var newElement = new
            {
                ondate = onDate,
                event_id = newEntry.GetInt("event_id"),
                type = newEntry.GetInt("type"),
                value = value
            }.ToBsonDocument();
            accumulator.AddDocumentArrayItem("events", newElement);
            return newElement;
        }

    }
}
