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
         

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })] //was 6
        public async void ExtractEventValueFeatures(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter() { Delimiter = ';' });
            var type = fileSource.GetTypeDefinition() as IntegrationTypeDefinition;
            Assert.NotNull(type);
            var harvester = new Peeralize.Service.Harvester(10);
            harvester.AddPersistentType(fileSource, _appId, true);
            harvester.LimitEntries(1000);

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
            var featureGenerator = new FeatureGenerator((doc) => helper
                .GetTopRatedFeatures(doc["uuid"].ToString(), VisitTypedValue, 10)
                .Select((x, i) => new KeyValuePair<string, double>($"Document._has_type_val_{i}", x)));
            var updateCreator = new TransformBlock<DocumentFeatures, FindAndModifyArgs<IntegratedDocument>>((x) =>
            {
                return new FindAndModifyArgs<IntegratedDocument>()
                {
                    Query = Builders<IntegratedDocument>.Filter.And(
                                Builders<IntegratedDocument>.Filter.Eq("Document.uuid", x.Document["uuid"].ToString()),
                                Builders<IntegratedDocument>.Filter.Eq("Document.noticed_date", x.Document.GetDate("noticed_date"))),
                    Update = x.Features.ToMongoUpdate<IntegratedDocument>()
                };
            });
            var updateBatcher = new MongoUpdateBatch<IntegratedDocument>(_documentStore, 300);
            
            featureGenerator.Block.LinkTo(updateCreator);
            updateCreator.LinkTo(updateBatcher.Block);
            var onComplete = new TransformBlock<IntegratedDocument, IntegratedDocument>(doc =>
            {
                featureGenerator.Block.Post(doc);
                return doc;
            });
            grouper.LinkOnComplete(onComplete);

//            grouper.LinkOnComplete(new IntegrationActionBlock(_appId, (block, doc) =>
//            {
//                var uuid = doc["uuid"].ToString();
//                var noticedDate = doc.GetDate("noticed_date");
//                var topTypeValues = helper.GetTopRatedFeatures(uuid, VisitTypedValue, 10).ToList();
//                
//                var query = Query.And(Query.EQ("Document.uuid", uuid), Query.EQ("Document.noticed_date", noticedDate));
//                var newUpdate = new UpdateBuilder();
//                for (int i = 0; i < 10; i++)
//                {
//                    var featureVal = topTypeValues[i];
//                    newUpdate.Set($"Document.has_type_val_{i}", featureVal);
//                }
//                UpdateFeatures(_appId, query, newUpdate);
//            }));

            harvester.SetDestination(grouper);
            //harvester.AddType(type, fileSource);
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
