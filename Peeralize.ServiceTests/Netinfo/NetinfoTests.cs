﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.db.DB.RDS;
using nvoid.db.Extensions;
using nvoid.extensions;
using Peeralize.Service;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Models;
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
        private CrossSiteAnalyticsHelper _helper;
        private IMongoCollection<IntegratedDocument> _documentStore;
        private string _appId; 
        private DateHelper _dateHelper;


        public NetinfoTests(ConfigurationFixture fixture)
        {
            _config = fixture;
            _helper = new CrossSiteAnalyticsHelper();
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>().MongoDb();
            _appId = "123123123";
            _dateHelper = new DateHelper();
        }

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\JoinedData"})]
        public async void ImportData(string inputDirectory)
        {
            try
            { 
                var currentDir = Environment.CurrentDirectory;

                inputDirectory = Path.Combine(currentDir, inputDirectory);
                Console.WriteLine($"Parsing data in: {inputDirectory}");
                var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter() { Delimiter = ';' });
                var harvester = new Harvester(10);
                var type = harvester.AddPersistentType(fileSource, _appId, true);
                var importCollectionId = Guid.NewGuid().ToString();
                var mlist = new MongoList(DBConfig.GetGeneralDatabase(), importCollectionId);
                mlist.Truncate();
                //harvester -> documentCreator -> inserter
                var batchSize = 30000;
                var executionOptions = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = batchSize,
                };
                var inputTarget = new BufferBlock<ExpandoObject>(executionOptions);
                var inserter = new MongoInsertBatch<BsonDocument>(mlist.Records, batchSize);
                var documentCreator = new TransformBlock<ExpandoObject, BsonDocument>(x =>
                {
                    var doc = new BsonDocument();
                    foreach (var pair in x) doc.Set(pair.Key, BsonValue.Create(pair.Value));
                    return doc;
                }, executionOptions);

                inputTarget.LinkTo(documentCreator, new DataflowLinkOptions { PropagateCompletion = true });
                documentCreator.LinkTo(inserter.BatchBlock);
                documentCreator.Completion.ContinueWith(x =>
                {
                    inserter.Trigger();
                    inserter.Complete();
                }); 
                var result = await harvester.ReadAll(inputTarget);
                Task.WaitAll(inserter.Completion, documentCreator.Completion, inputTarget.Completion);
                Debug.WriteLine($"Imported: {result.ProcessedEntries} files({result.ProcessedShards}) {importCollectionId}");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\JoinedData" })]
        public async void CustomInsertExample(string inputDirectory)
        {
            var currentDir = Environment.CurrentDirectory;

            inputDirectory = Path.Combine(currentDir, inputDirectory);
            Console.WriteLine($"Parsing data in: {inputDirectory}");
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter() { Delimiter = ';' });
            var harvester = new Harvester(10);
            var type = harvester.AddPersistentType(fileSource, _appId, true);

            var importCollectionId = Guid.NewGuid().ToString();
            var rawEventsCollection = new MongoList(DBConfig.GetGeneralDatabase(), importCollectionId);
            var reducedEventsCollection = new MongoList(DBConfig.GetGeneralDatabase(), importCollectionId+"_reduced");
            rawEventsCollection.Truncate();

            var batchesInserted = 0;
            var batchSize = 30000;
            var executionOptions = new ExecutionDataflowBlockOptions {  BoundedCapacity = 1,  };
            var locked = false;
            var insert = new ActionBlock<BsonDocument[]>(x =>
            {
                Debug.WriteLine($"Inserting batch {batchesInserted+1} [{x.Length}]");
                rawEventsCollection.Records.InsertMany(x);
                Interlocked.Increment(ref batchesInserted);
                Debug.WriteLine($"Inserted batch {batchesInserted}");
                locked = false;
            }, executionOptions);
            var transformerBlock = new TransformBlock<ExpandoObject[], BsonDocument[]>(values =>
            {
                var output = new BsonDocument[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    var val = values[i];
                    var doc = new BsonDocument();
                    foreach (var pair in val)
                    {
                        if (pair.Key == "ondate")
                        {
                            doc.Set(pair.Key, BsonValue.Create(DateTime.Parse(pair.Value.ToString())));
                        }
                        else
                        {
                            doc.Set(pair.Key, BsonValue.Create(pair.Value));
                        }
                    }
                    output[i] = doc;
                }
                return output;
            }, new ExecutionDataflowBlockOptions{ BoundedCapacity = 1 });
            var batcher = BatchedBlockingBlock<ExpandoObject>.CreateBlock(batchSize); 
            batcher.LinkTo(transformerBlock, new DataflowLinkOptions { PropagateCompletion = true });
            //insertBat.LinkTo(transformerBlock, new DataflowLinkOptions { PropagateCompletion = true });
            transformerBlock.LinkTo(insert, new DataflowLinkOptions { PropagateCompletion = true });
             
            var result = await harvester.ReadAll(batcher);
            harvester = null;
            //Reduce
            await Task.WhenAll(insert.Completion, transformerBlock.Completion);
            rawEventsCollection.EnsureIndex("ondate");
            var map = @"
function () {    
  var time = parseInt((this.ondate.getTime() / 1000) / (60 * 60 * 24));
  var eventData = [{ ondate : this.ondate, value : this.value, type : this.type }];
  emit({ uuid : this.uuid, day : time }, { 
    uuid : this.uuid,
    events : eventData
  });
}";
            var reduce = @"
function (key, values) {
  var elements = [];
  var startTime = null;
  values.forEach(function(a){ 
    elements = a.events;
    if(startTime==null && elements.length>0) startTime = elements[0].ondate;
  }); 
  var day = parseInt((startTime.getTime() / 1000) / (60 * 60 * 24))
  return {uuid : key.uuid, day : day, noticed_date : startTime, events : elements };
}";
            var reduceCursor = rawEventsCollection.Records.MapReduce<BsonDocument>(map, reduce, new MapReduceOptions<BsonDocument,BsonDocument>
            {
                Sort = Builders<BsonDocument>.Sort.Ascending("ondate")
            });
            var reduceBatcher = BatchedBlockingBlock<BsonDocument>.CreateBlock(30000);
            var reducedInserter = new ActionBlock<BsonDocument[]>(reducedElements =>
            {
                reducedEventsCollection.Records.InsertMany(reducedElements);
            });
            reduceBatcher.LinkTo(reducedInserter, new DataflowLinkOptions {PropagateCompletion = true});
            foreach (var element in reduceCursor.ToEnumerable())
            {
                reduceBatcher.SendChecked(element);
            }
            await Task.WhenAll(reduceBatcher.Completion, reducedInserter.Completion);
            Debug.WriteLine(importCollectionId);
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
            //harvester.AddPersistentType(fileSource, _appId, true);

            var grouper = new GroupingBlock(_appId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.DaysTotal()}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                (acc, doc) => AccumulateUserDocument(acc, doc));
            grouper.Helper = _helper;
            //Group the users
            // create features for each user -> create Update -> batch update
            var featureHelper = new FeatureGeneratorHelper() { Helper = _helper, TargetDomain = "ebag.bg"};
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
            var updateBatcher = new MongoUpdateBatch<IntegratedDocument>(_documentStore, 3000);
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
        [InlineData(new object[] {"3e0f12d2-2c20-40f8-ac9c-031d3a33a216_reduced2", "TestData\\Ebag\\demograpy.csv" })]
        public async void ExtractEntitiesFromReducedCollection(string collectionName, string demographySheet)
        {
            var appId = "123123123";
            MongoSource source = MongoSource.CreateFromCollection(collectionName, new BsonFormatter());
            source.SetProjection(x =>
            {
                if (!x["value"].AsBsonDocument.Contains("day")) x["value"]["day"] = x["_id"]["day"];
                return x["value"] as BsonDocument;
            });
            var harvester = new Peeralize.Service.Harvester(10);
            var type = harvester.AddPersistentType("NetInfoUserFeatures_7_8", appId, source); 

            var dictEval = new EvalDictionaryBlock(
                (document) => $"{document.GetString("uuid")}_{document.GetInt("day")}",
                (rootElement, newDoc) => AccumulateUserDocument(rootElement, newDoc, false),
                (rootElement) => rootElement.GetArray("events"));
            _helper = new CrossSiteAnalyticsHelper(dictEval.Elements);
            dictEval.Helper = _helper;

            var featureHelper = new FeatureGeneratorHelper() { Helper = _helper, TargetDomain = "ebag.bg" };
            var featureGenerator = new FeatureGenerator(new Func<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>[]
            {
                featureHelper.GetFeatures,
                featureHelper.GetAvgTimeBetweenSessionFeatures
            }, 16); 
            var featureGeneratorBlock = featureGenerator.CreateFeaturesBlock();

            var demographyImporter = new EntityDataImporter(demographySheet, true);
            //demographyImporter.SetEntityRelation((input, x) => input[0] == x.Document["uuid"]);
            demographyImporter.SetDataKey((input) => input[0]);
            demographyImporter.SetEntityKey((input) => input.GetString("uuid"));
            demographyImporter.JoinOn(JoinDemography);
            demographyImporter.Map();

            var insertCreator = new TransformBlock<DocumentFeatures, IntegratedDocument>((x) =>
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
                doc.TypeId = type.Id.Value;
                doc.UserId = appId;
                x.Features = null; 
                return doc;
            });
            var insertBatcher = new MongoInsertBatch<IntegratedDocument>(_documentStore, 3000);

            demographyImporter.Helper = _helper; 
            demographyImporter.LinkTo(featureGeneratorBlock);
            featureGeneratorBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true }); 
            insertCreator.LinkTo(insertBatcher.BatchBlock, new DataflowLinkOptions{ PropagateCompletion = true}); 

            dictEval.LinkOnComplete(demographyImporter);
            dictEval.AddFlowCompletionTask(insertBatcher.Completion);

            harvester.SetDestination(dictEval);
            var result = await harvester.Synchronize();
            Debug.WriteLine(result.ProcessedEntries);
        }


        [Theory]
        [InlineData(new object[] {"3e0f12d2-2c20-40f8-ac9c-031d3a33a216_reduced2"})]
        public async void ParseEntitySessionsDumpCollection(string collectionName)
        { 
            MongoSource source = MongoSource.CreateFromCollection(collectionName, new BsonFormatter());
            source.SetProjection(x =>
            {
                if (!x["value"].AsBsonDocument.Contains("day")) x["value"]["day"] = x["_id"]["day"];
                return x["value"] as BsonDocument;
            });

            var appId = "123123123";
            var harvester = new Peeralize.Service.Harvester(20); 
            harvester.AddPersistentType("NetInfoUserFeatures_7_8", appId, source);
            var typeDef = IntegrationTypeDefinition.CreateFromType<DomainUserSessionCollection>("NetInfoUserSessions_7_8", appId);
            typeDef.AddField("is_paying", typeof(int));
            IntegrationTypeDefinition existingTypeDef;
            if (!IntegrationTypeDefinition.TypeExists(typeDef, appId, out existingTypeDef)) typeDef.Save();
            else typeDef = existingTypeDef;

            var dictEval = new EvalDictionaryBlock(
                (document) =>
                {
                    return $"{document.GetString("uuid")}_{document.GetInt("day")}";
                },
                (rootElement, newDoc) => AccumulateUserDocumentLite(rootElement, newDoc),
                (rootElement) => rootElement.GetArray("events"));
            dictEval.Helper = _helper = new CrossSiteAnalyticsHelper(dictEval.Elements);
            //Session block
            //Group the users
            var sessionDocBlock = new TransformBlock<IntegratedDocument, IntegratedDocument>((IntegratedDocument userBlock) =>
            {
                var userDocument = userBlock.GetDocument();
                var userIsPaying = userDocument.Contains("is_paying") &&
                                   userDocument["is_paying"].AsInt32 == 1;
                //We're only interested in paying users
                //if (userIsPaying) continue;
                var uuid = userDocument["uuid"].ToString();
                var dateNoticed = DateTime.Parse(userDocument["noticed_date"].ToString());
                DateTime g_timestamp = userDocument["noticed_date"].AsDateTime.StartOfWeek(DayOfWeek.Monday);
                userDocument["events"] =
                    ((BsonArray)userDocument["events"])
                    .OrderBy(x => DateTime.Parse(x["ondate"].ToString()))
                    .ToBsonArray();
                var sessions = CrossSiteAnalyticsHelper.GetWebSessions(userBlock).ToList();
                var sessionWrapper = new DomainUserSessionCollection(sessions);
                sessionWrapper.UserId = uuid;
                sessionWrapper.Created = dateNoticed;

                var document = IntegratedDocument.FromType(sessionWrapper, typeDef, appId);
                var documentBson = document.GetDocument();
                documentBson["is_paying"] = userIsPaying ? 1 : 0;
                documentBson["g_timestamp"] = g_timestamp;
                return document;
            });
            var sessionBatcher = new MongoInsertBatch<IntegratedDocument>(_documentStore, 10000);
            
            dictEval.LinkOnCompleteEx(sessionDocBlock);
            sessionDocBlock.LinkTo(sessionBatcher.BatchBlock, new DataflowLinkOptions{ PropagateCompletion=true });
            dictEval.AddFlowCompletionTask(sessionBatcher.Completion);
            dictEval.AddFlowCompletionTask(sessionDocBlock.Completion);
            harvester.SetDestination(dictEval);
            var syncResults = await harvester.Synchronize();

            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all docs in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
            //Task.WaitAll(grouper.Completion, featureGen.Completion, );
            //await grouper.Completion;
            //Console.ReadLine(); // TODO: Fix dataflow action after grouping of all users
        }

        private void DumpUsergroupSessionsToMongo(string userAppId, EvalDictionaryBlock usersData)
        {
            var entityBlock = usersData;
            var typeDef = IntegrationTypeDefinition.CreateFromType<DomainUserSessionCollection>("NetInfoUserSessions_7_8",userAppId);
            typeDef.AddField("is_paying", typeof(int));
            IntegrationTypeDefinition existingTypeDef;
            if (!IntegrationTypeDefinition.TypeExists(typeDef, userAppId, out existingTypeDef)) typeDef.Save();
            else typeDef = existingTypeDef;
            var entityPairs = entityBlock.Elements;
            try
            {
                foreach (var userDayInfoPairs in entityPairs)
                {
                    try
                    {
                        var user = entityPairs[userDayInfoPairs.Key];
                        var userDocument = user.GetDocument();
                        var userIsPaying = userDocument.Contains("is_paying") &&
                                           userDocument["is_paying"].AsInt32 == 1;
                        //We're only interested in paying users
                        //if (userIsPaying) continue;

                        var uuid = userDocument["uuid"].ToString();
                        var dateNoticed = DateTime.Parse(userDocument["noticed_date"].ToString());
                        userDocument["events"] =
                            ((BsonArray)userDocument["events"])
                            .OrderBy(x => DateTime.Parse(x["ondate"].ToString()))
                            .ToBsonArray();
                        var sessions = CrossSiteAnalyticsHelper.GetWebSessions(user).ToList();
                        var sessionWrapper = new DomainUserSessionCollection(sessions);
                        sessionWrapper.UserId = uuid;
                        sessionWrapper.Created = dateNoticed;

                        var document = IntegratedDocument.FromType(sessionWrapper, typeDef, userAppId);
                        var documentBson = document.GetDocument();
                        documentBson["is_paying"] = userIsPaying ? 1 : 0;
                        document.TypeId = typeDef.Id.Value;
                        //document.Save();
                    }
                    catch (Exception ex2)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }


        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\JoinedData", "TestData\\Ebag\\demograpy.csv" })]
        public async void ExtractEntityFromDirectory(string inputDirectory, string demographySheet)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter(){ Delimiter = ';' });

            var userId = "123123123"; 
            var harvester = new Peeralize.Service.Harvester(10);
            var type = harvester.AddPersistentType(fileSource, userId, true);
            
            var grouper = new GroupingBlock(_appId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.DaysTotal()}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                (acc, newDoc) => AccumulateUserDocument(acc,newDoc, true));

            var helper = new CrossSiteAnalyticsHelper(grouper.EntityDictionary);
            var featureHelper = new FeatureGeneratorHelper() { Helper = helper, TargetDomain = "ebag.bg" };
            var featureGenerator = new FeatureGenerator(featureHelper.GetFeatures, 12);
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
            insertCreator.LinkTo(insertBatcher.BatchBlock);

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
            grouper.Helper = _helper;
            grouper.LinkTo(new ActionBlock<IntegratedDocument>((x) =>
            {
                CollectTypeValuePair(x.GetString("uuid"), x.GetDocument());
            }));
            //Group the users
            // create features for each user -> create Update -> batch update
            var featureGenerator = new FeatureGenerator((doc) => 
                _helper.GetTopRatedFeatures(doc["uuid"].ToString(), VisitTypedValue, 10)
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
        private void CollectTypeValuePair(string userId, BsonDocument arg)
        {
            if (arg.IsNumeric("value"))
            {
                var intval = arg.GetInt("value");
                var ev = arg.GetInt("type");
                var key_value = $"{ev}_{intval}";  
                if (string.IsNullOrEmpty(userId)) return;
                _helper.AddRatingFeature(VisitTypedValue, userId, key_value); 
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

        private object AccumulateUserDocumentLite(IntegratedDocument accumulator, BsonDocument newEntry)
        {
            var value = newEntry.GetString("value");
            if (value.Contains("payments/finish") && value.ToHostname().Contains("ebag.bg"))
            {

                accumulator["is_paying"] = 1;
            }
            return newEntry;
        }

        private object AccumulateUserDocument(IntegratedDocument accumulator, BsonDocument newEntry, bool appendEvent = true)
        {
            var value = newEntry.GetString("value");
            var onDate = newEntry.GetDate("ondate").Value;
            var uuid = accumulator.GetString("uuid");
            CollectTypeValuePair(uuid, newEntry);
//            var newElement = new
//            {
//                ondate = newEntry.GetDate("ondate"),
//                event_id = newEntry.GetInt("event_id"),
//                type = newEntry.GetInt("type"),
//                value = newEntry.GetString("value")
//            }.ToBsonDocument();
              
            var pageHost = value.ToHostname();
            var pageSelector = pageHost;
            var isNewPage = false;
            if (!_helper.Stats.ContainsPage(pageHost))
            {
                _helper.Stats.AddPage(pageSelector, new PageStats()
                {
                    Page = value
                });
                isNewPage = true;
            }
            _helper.Stats[pageSelector].PageVisitsTotal++;

            if (appendEvent)
            {
                accumulator.AddDocumentArrayItem("events", newEntry);
            }
            if (value.Contains("payments/finish") && value.ToHostname().Contains("ebag.bg"))
            {
                if (_dateHelper.IsHoliday(onDate))
                {
                    _helper.PurchasesOnHolidays.Add(newEntry);
                }
                else if (_dateHelper.IsHoliday(onDate.AddDays(1)))
                {
                    _helper.PurchasesBeforeHolidays.Add(newEntry);
                }
                else if (onDate.DayOfWeek == DayOfWeek.Friday)
                {
                    _helper.PurchasesBeforeWeekends.Add(newEntry);
                }
                else if (onDate.DayOfWeek > DayOfWeek.Friday)
                {
                    _helper.PurchasesInWeekends.Add(newEntry);
                }
                _helper.Purchases.Add(newEntry);
                accumulator["is_paying"] = 1; 
            }
            return newEntry;
        } 

    }
}
