using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Donut;
using Donut.Blocks;
using Donut.Caching;
using Donut.Data;
using Donut.Data.Format;
using Donut.Encoding;
using Donut.FeatureGeneration;
using Donut.Features;
using Donut.IntegrationSource;
using Donut.Lex.Data;
using Donut.Orion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using nvoid.db.DB;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.db.Extensions;
using nvoid.extensions;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Batching;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Analytics;
using Netlyt.Service.Data;
using Xunit;
using Netlyt.ServiceTests.Fixtures;
using Romanian;
using StackExchange.Redis;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.ServiceTests.Netinfo
{
    [Collection("DonutTests")]
    public class NetinfoTests
    {
        public const byte VisitTypedValue = 1;
        private DonutConfigurationFixture _fixture;
        //private CrossSiteAnalyticsHelper _helper;
        private IMongoCollection<IntegratedDocument> _documentStore;
        private ApiAuth _appId;
        private DynamicContextFactory _contextFactory;
        private ApiService _apiService;
        private IIntegrationService _integrationService;
        private IRedisCacher _redisCacher;
        private IConnectionMultiplexer _redisConnection;
        private IDatabaseConfiguration _dbConfig;
        private ApiAuth _appAuth;
        private ManagementDbContext _db;
        private OrionContext _orion;
        private UserService _userService;
        private User _user;
        private CompilerService _compiler;
        private IRedisCacher _cacher;
        private ServiceProvider _serviceProvider;

        public NetinfoTests(DonutConfigurationFixture fixture)
        {
            _fixture = fixture;
            //_helper = new CrossSiteAnalyticsHelper();
            _documentStore = MongoHelper.GetCollection<IntegratedDocument>(typeof(IntegratedDocument).Name);
            //typeof(IntegratedDocument).GetDataSource<IntegratedDocument>().AsMongoDbQueryable(); 
            _contextFactory = new DynamicContextFactory(() => _fixture.CreateContext());
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IIntegrationService>();
            _appId = _apiService.Generate();
            _apiService.Register(_appId);
            _redisCacher = fixture.GetService<IRedisCacher>();
            _dbConfig = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            _userService = fixture.GetService<UserService>();
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _db = fixture.GetService<ManagementDbContext>();
            _orion = fixture.GetService<OrionContext>();
            _compiler = fixture.GetService<CompilerService>();
            _cacher = fixture.GetService<IRedisCacher>();
            _serviceProvider = fixture.ServiceProvider;//GetService<IServiceProvider>();
            //            var orionCtx = new Mock<IOrionContext>();
            //            orionCtx.Setup(m => m.Query(It.IsAny<OrionQuery>())).ReturnsAsync(new JObject { {"id", 1} });
            //            _orion = orionCtx.Object;
            _user = _userService.GetByApiKey(_appAuth);
            if (_user == null)
            {
                _user = new User() { FirstName = "tester1231", Email = "mail@lol.co" };
                _userService.CreateUser(_user, "Password-IsStrong!", _appAuth).Wait();
            }
        }

        [Theory]
        [InlineData(new object[] { "Res\\TestData\\pollution_data.csv" })]
        public async Task TestMongoDonutJointFeaturesLong(string sourceFile)
        {
            var modelName = "NetinfoJoinedData";
            IInputSource inputSource;
            var integration = _db.Integrations
                .Include(x=>x.Models)
                .Include(x=>x.Fields)
                .Include(x=>x.AggregateKeys)
                .FirstOrDefault(x=>x.Id == 250);

            var ops = integration.AggregateKeys.Select(x =>  x.Operation ).ToList();
            var modId = integration.Models.FirstOrDefault()?.ModelId;
            var model = _db.Models
                .Include(x=> x.DonutScript)
                .Include(x=>x.DataIntegrations)
                .FirstOrDefault(x=>x.Id==modId);
            //Run file integration
            var features = @"paid
SUM(NetinfoJoinedData_csv.event)
SUM(NetinfoJoinedData_csv.type)
STD(NetinfoJoinedData_csv.event)
STD(NetinfoJoinedData_csv.type)
MAX(NetinfoJoinedData_csv.event)
MAX(NetinfoJoinedData_csv.type)
SKEW(NetinfoJoinedData_csv.event)
SKEW(NetinfoJoinedData_csv.type)
MIN(NetinfoJoinedData_csv.event)
MIN(NetinfoJoinedData_csv.type)
MEAN(NetinfoJoinedData_csv.event)
MEAN(NetinfoJoinedData_csv.type)
COUNT(NetinfoJoinedData_csv)
NUM_UNIQUE(NetinfoJoinedData_csv.value0)
NUM_UNIQUE(NetinfoJoinedData_csv.value1)
NUM_UNIQUE(NetinfoJoinedData_csv.value2)
NUM_UNIQUE(NetinfoJoinedData_csv.value3)
NUM_UNIQUE(NetinfoJoinedData_csv.value4)
NUM_UNIQUE(NetinfoJoinedData_csv.value5)
NUM_UNIQUE(NetinfoJoinedData_csv.value6)
NUM_UNIQUE(NetinfoJoinedData_csv.value7)
MODE(NetinfoJoinedData_csv.value0)
MODE(NetinfoJoinedData_csv.value1)
MODE(NetinfoJoinedData_csv.value2)
MODE(NetinfoJoinedData_csv.value3)
MODE(NetinfoJoinedData_csv.value4)
MODE(NetinfoJoinedData_csv.value5)
MODE(NetinfoJoinedData_csv.value6)
MODE(NetinfoJoinedData_csv.value7)
DAY(first_NetinfoJoinedData_csv_time)
YEAR(first_NetinfoJoinedData_csv_time)
MONTH(first_NetinfoJoinedData_csv_time)
WEEKDAY(first_NetinfoJoinedData_csv_time)
NUM_UNIQUE(NetinfoJoinedData_csv.DAY(ondate))
NUM_UNIQUE(NetinfoJoinedData_csv.YEAR(ondate))
NUM_UNIQUE(NetinfoJoinedData_csv.MONTH(ondate))
NUM_UNIQUE(NetinfoJoinedData_csv.WEEKDAY(ondate))
MODE(NetinfoJoinedData_csv.DAY(ondate))
MODE(NetinfoJoinedData_csv.YEAR(ondate))
MODE(NetinfoJoinedData_csv.MONTH(ondate))
MODE(NetinfoJoinedData_csv.WEEKDAY(ondate))
";
            string[] featureBodies = features.Split('\n');
            string donutName = $"{model.ModelName}Donut";
            DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, model.TargetAttribute, model.GetRootIntegration(), featureBodies);
            foreach (var modelIgn in model.DataIntegrations)
            {
                dscript.AddIntegrations(modelIgn.Integration);
            }
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, model.ModelName, out donutType, out donutContextType, out donutFEmitterType);
            Assert.NotNull(assembly);

            //Create a donut and a donutRunner
            var donutMachine = DonutGeneratorFactory.Create<IntegratedDocument>(donutType, donutContextType, integration,
                _cacher, _serviceProvider);
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(10000);
            donut.ReplayInputOnFeatures = true;
            var harvester = new Harvester<IntegratedDocument>(10);
            //harvester.AddIntegration(integration, inputSource);

            IDonutRunner<IntegratedDocument> donutRunner = DonutRunnerFactory.CreateByType(donutType, donutContextType, harvester, 
                _dbConfig, integration.FeaturesCollection);

            var featureGenerator = FeatureGeneratorFactory<IntegratedDocument>.Create(donut, donutFEmitterType);

            var result = await donutRunner.Run(donut, featureGenerator);
            integration.GetMongoFeaturesCollection<BsonDocument>().Drop();
            integration.GetMongoCollection<BsonDocument>().Drop();
        }


        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\NewData" })]
        public async void ImportData(string inputDirectory)
        {
            try
            {
                var currentDir = Environment.CurrentDirectory;
                inputDirectory = Path.Combine(currentDir, inputDirectory);
                Console.WriteLine($"Parsing data in: {inputDirectory}");
                var importCollectionId = Guid.NewGuid().ToString();
                var mlist = new MongoList(_dbConfig.Name, importCollectionId, _dbConfig.GetUrl());
                //var altList = RemoteDataSource.GetMongoDb<BsonDocument>(importCollectionId);

                var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>() { Delimiter = ';' });
                var harvester = new Harvester<ExpandoObject>(10);
                var type = harvester.AddIntegrationSource(fileSource, _appId, null);

                mlist.Truncate();
                //harvester -> documentCreator -> inserter
                var batchSize = (uint)30000;
                var executionOptions = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = (int)batchSize,
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
                var tx = documentCreator.Completion.ContinueWith(x =>
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
        [InlineData(new object[]
        {
            "TestData\\Ebag\\NewJoin", "NetInfoUserFeatures_7_8"
        })]
        public async void CustomInsertExample(string inputDirectory, string typeName)
        {
            var currentDir = Environment.CurrentDirectory;
            inputDirectory = Path.Combine(currentDir, inputDirectory);
            Console.WriteLine($"Parsing data in: {inputDirectory}");
            var importTask = new DataImportTask<ExpandoObject>(new DataImportTaskOptions
            {
                Source = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>() { Delimiter = ';' }),
                ApiKey = _appId,
                IntegrationName = typeName,
                ThreadCount = 10
            }.AddIndex("ondate"));
            var importResult = await importTask.Import();
            var map = @"
function () {    
  var time = parseInt((this.ondate.getTime() / 1000) / (60 * 60 * 24));
  var eventData = [{ ondate : this.ondate, value : this.value, type : this.type }];
  emit({ uuid : this.uuid, day : time }, { 
    uuid : this.uuid,
    day : time,
    noticed_date : this.ondate,
    events : eventData
  });
}";
            var reduce = @"
function (key, values) {
  var elements = [];
  var startTime = null;
  values.forEach(function(a){ 
	for(var i=0; i<a.events.length;i++) elements.push(a.events[i]);    
  });  
  if(startTime==null && elements.length>0) startTime = elements[0].ondate;
  return {
uuid : key.uuid,
day : key.day,
noticed_date : startTime,
events : elements };
}";
            var mapReduceOptions = new MapReduceOptions<BsonDocument, BsonDocument>
            {
                Sort = Builders<BsonDocument>.Sort.Ascending("ondate"),
                JavaScriptMode = true,
                OutputOptions = MapReduceOutputOptions.Replace(importTask.OutputDestinationCollection.ReducedOutputCollection)
            };
            var reduceCursor = await importResult.Collection.MapReduceAsync(map, reduce, mapReduceOptions);
        }


        /// <summary>
        /// TODO: Make grouping keys(day) be day since unix timestamp start
        /// </summary>
        /// <param name="inputDirectory"></param>
        /// <param name="targetDomain"></param>
        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void ExtractAvgTimeBetweenVisitFeatures(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>() { Delimiter = ';' });
            var harvester = new Harvester<IntegratedDocument>(10);
            //harvester.AddPersistentType(fileSource, _appId, true);

            var grouper = new GroupingBlock<IntegratedDocument>(_appId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.DaysTotal()}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                (acc, doc) => AccumulateUserDocument(acc, doc));
            //grouper.Helper = _helper;
            //Group the users
            // create features for each user -> create Update -> batch update
            //var featureHelper = new NetinfoFeatureGeneratorHelper() { Helper = _helper, TargetDomain = "ebag.bg"};
            var featureHelper = new NetinfoFeatureGeneratorHelper() { TargetDomain = "ebag.bg" };
            var featureGenerator = new FeatureGenerator<IntegratedDocument>(featureHelper.GetAvgTimeBetweenSessionFeatures, 8);
            var updateCreator = new TransformBlock<FeaturesWrapper<IntegratedDocument>, FindAndModifyArgs<IntegratedDocument>>((docFeatures) =>
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
            grouper.AddCompletionTask(featuresBlock.Completion);
            grouper.AddCompletionTask(updateBatcher.Block.Completion);
            grouper.LinkOnComplete(new TransformBlock<IntegratedDocument, IntegratedDocument>(doc =>
            {
                featuresBlock.Post(doc);
                return doc;
            }));
            harvester.SetDestination(grouper);
            var completion = await harvester.Run();
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
        }


        [Theory]
        [InlineData(new object[] { "057cecc6-0c1b-44cd-adaa-e1089f10cae8_reduced", "TestData\\Ebag\\demograpy.csv" })]
        public async void ExtractEntitiesFromReducedCollection(string collectionName, string demographySheet)
        {
            MongoSource<ExpandoObject> source = MongoSource.CreateFromCollection(collectionName, new BsonFormatter<ExpandoObject>());
            source.SetProjection(x =>
            {
                if (!((IDictionary<string, object>)x).ContainsKey("value")) ((dynamic)x).value.day = ((dynamic)x)._id.day;
                return ((dynamic)x).value as ExpandoObject;
            });
            var harvester = new Harvester<IntegratedDocument>(10);
            var type = harvester.AddIntegrationSource(source, _appId, "NetInfoUserFeatures_7_8_1");

            var dictEval = new EvalDictionaryBlock(
                (document) => $"{document.GetString("uuid")}_{document.GetInt("day")}",
                (rootElement, newDoc) => AccumulateUserDocument(rootElement, newDoc, false),
                (rootElement) => rootElement.GetArray("events"));
            //_helper = new CrossSiteAnalyticsHelper(dictEval.Elements);
            //dictEval.Helper = _helper;

            //var featureHelper = new NetinfoFeatureGeneratorHelper() { Helper = _helper, TargetDomain = "ebag.bg" };
            var featureHelper = new NetinfoFeatureGeneratorHelper() { TargetDomain = "ebag.bg" };
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

            //demographyImporter.Helper = _helper;
            dictEval.LinkOnComplete(demographyImporter);
            demographyImporter.LinkTo(featureGeneratorBlock);
            featureGeneratorBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            insertCreator.LinkTo(insertBatcher.BatchBlock, new DataflowLinkOptions { PropagateCompletion = true });

            dictEval.AddCompletionTask(insertBatcher.Completion);

            harvester.SetDestination(dictEval);
            var result = await harvester.Run();
            Debug.WriteLine(result.ProcessedEntries);
        }



        [Theory]
        [InlineData(new object[] { "057cecc6-0c1b-44cd-adaa-e1089f10cae8_reduced" })]
        public async void ParseEntitySessionsDumpCollection(string collectionName)
        {
            MongoSource<ExpandoObject> source = MongoSource.CreateFromCollection(collectionName, new BsonFormatter<ExpandoObject>());
            source.SetProjection(x =>
            {
                if (!((IDictionary<string, object>)x).ContainsKey("value")) ((dynamic)x).value.day = ((dynamic)x)._id.day;
                return ((dynamic)x).value as ExpandoObject;
            });

            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Harvester<IntegratedDocument>(20);
            harvester.AddIntegrationSource(source, apiObj, "NetInfoUserFeatures_7_8");
            var typeDef = DataIntegration.Factory.CreateFromType<DomainUserSessionCollection>("NetInfoUserSessions_7_8", apiObj);
            typeDef.AddField("is_paying", typeof(int));
            //DataIntegration existingTypeDef;
            //if (!_integrationService.IntegrationExists(typeDef, appId, out existingTypeDef)) typeDef.Save();
            //else typeDef = existingTypeDef;
            var dictEval = new EvalDictionaryBlock(
                (document) => $"{document.GetString("uuid")}_{document.GetInt("day")}",
                (rootElement, newDoc) => AccumulateUserDocumentLite(rootElement, newDoc),
                (rootElement) => rootElement.GetArray("events"));
            //dictEval.Helper = _helper = new CrossSiteAnalyticsHelper(dictEval.Elements);
            //Session block
            //Group the users
            var sessionDocBlock = new TransformBlock<IntegratedDocument, IntegratedDocument>((IntegratedDocument userBlock) =>
            {
                var userDocument = userBlock.GetDocument(); ;
                var userIsPaying = userDocument.Contains("is_paying") &&
                                   userDocument["is_paying"].AsInt32 == 1;
                var uuid = userDocument["uuid"].ToString();
                var dateNoticed = DateTime.Parse(userDocument["noticed_date"].ToString());
                DateTime g_timestamp = userDocument["noticed_date"].ToUniversalTime().StartOfWeek(DayOfWeek.Monday);
                userDocument["events"] =
                    ((BsonArray)userDocument["events"])
                    .OrderBy(x => DateTime.Parse(x["ondate"].ToString()))
                    .ToBsonArray();
                IList<DomainUserSession> sessions = null;//NetinfoDonutfile.GetWebSessions(userBlock).ToList();
                var sessionWrapper = new DomainUserSessionCollection(sessions) { UserId = uuid, Created = dateNoticed };
                var document = IntegratedDocument.FromType<DomainUserSessionCollection, IntegratedDocument>(sessionWrapper, typeDef, apiObj.Id);
                var documentBson = document.GetDocument();
                documentBson["is_paying"] = userIsPaying ? 1 : 0;
                documentBson["g_timestamp"] = g_timestamp;
                return document;
            });
            var sessionBatcher = new MongoInsertBatch<IntegratedDocument>(_documentStore, 10000);

            dictEval.LinkOnCompleteEx(sessionDocBlock);
            sessionDocBlock.LinkTo(sessionBatcher.BatchBlock, new DataflowLinkOptions { PropagateCompletion = true });
            dictEval.AddCompletionTask(sessionBatcher.Completion);
            dictEval.AddCompletionTask(sessionDocBlock.Completion);
            harvester.SetDestination(dictEval);
            var syncResults = await harvester.Run();
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all docs in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
            //Task.WaitAll(grouper.Completion, featureGen.Completion, );
            //await grouper.Completion;
            //Console.ReadLine(); // TODO: Fix dataflow action after grouping of all users
        }

        [Theory]
        [InlineData(new object[] { "IntegratedDocument" })]
        public async void ConstructTrees(string reducedSource)
        {
            MongoSource<ExpandoObject> source = MongoSource.CreateFromCollection(reducedSource, new BsonFormatter<ExpandoObject>());
            var harvester = new Harvester<IntegratedDocument>(20);
            var type = harvester.AddIntegrationSource(source, _appId, "NetInfoUserSessions_7_8");
            var batchSize = 10000;
            var updateBatchSize = (uint)10000;
            var recordLimit = 1000;
            source.Aggregate(source.CreateAggregate()
                .Match(new BsonDocument
                {
                    {"TypeId", type.Id.ToString()},
                    {"Document.is_paying", 0}
                })
                .Group(new BsonDocument
                {
                    {"_id" , "$Document.APIKey"},
                    {"day_count" , new BsonDocument{{ "$sum", 1 } }},
                    {"daily_sessions" , new BsonDocument{{"$push", "$Document.Sessions"}}},
                }).Limit(recordLimit));
            //Feed the builder with documents of distinct user_day
            var builder = new TreeBuilder(batchSize, "_id");
            var intDocRecords = new MongoList(_dbConfig.Name, "IntegratedDocument", _dbConfig.GetUrl()).Records;
            var treePayingUsers = TreeBuilder.BuildFromItems(from x in intDocRecords.AsQueryable()
                                                             where x["TypeId"] == type.Id.ToString() && x["Document.is_paying"] == 1
                                                             select x);
            var tweek = DateTime.Now;
            var treeUsersNotPurchasedInTWeek = TreeBuilder.BuildFromItems(from x in intDocRecords.AsQueryable()
                                                                          where
                                                                          x["TypeId"] == type.Id.ToString() &&
                                                                          x["Document.Created"] >= tweek && x["Document.Created"] <= (tweek + TimeSpan.FromDays(7)) &&
                                                                          x["Document.is_paying"] == 0
                                                                          select x);
            var treeUsersPurchasedAfterTWeek = TreeBuilder.BuildFromItems(from x in intDocRecords.AsQueryable()
                                                                          where
                                                                              x["TypeId"] == type.Id.ToString() &&
                                                                              x["Document.Created"] >= tweek && x["Document.Created"] <= (tweek + TimeSpan.FromDays(7)) &&
                                                                              x["Document.is_paying"] == 1
                                                                          select x);

            var featureGenerator = new FeatureGenerator<Tuple<BehaviourTree, IntegratedDocument>>(5);
            featureGenerator.AddGenerator(x =>
            {
                var pairs = new List<KeyValuePair<string, object>>();
                double simtime = treePayingUsers.LinScore(x.Item1, "time");
                double simfreq = treePayingUsers.LinScore(x.Item1, "frequency");

                pairs.Add(new KeyValuePair<string, object>("path_similarity_score", simtime));
                pairs.Add(new KeyValuePair<string, object>("path_similarity_score_time_spent", simfreq));

                //                pairs.Add(new KeyValuePair<string, object>("non_paying_s_time", simfreq));
                //                pairs.Add(new KeyValuePair<string, object>("non_paying_s_freq", simfreq));
                //                pairs.Add(new KeyValuePair<string, object>("paying_s_time", simfreq));
                //                pairs.Add(new KeyValuePair<string, object>("paying_s_freq", simfreq));
                return pairs;
            });
            var featureBlock = featureGenerator.CreateFeaturesBlock<TreeFeatures>();
            builder.LinkTo(featureBlock, new DataflowLinkOptions { PropagateCompletion = true });
            var updateCreator = new TransformBlock<TreeFeatures, FindAndModifyArgs<IntegratedDocument>>((x) =>
            {
                return new FindAndModifyArgs<IntegratedDocument>()
                {
                    Query = Builders<IntegratedDocument>.Filter.And(
                        Builders<IntegratedDocument>.Filter.Eq("Document.uuid", x.Document.Item2["_id"].ToString()),
                        Builders<IntegratedDocument>.Filter.Eq("TypeId", type.Id.ToString())),
                    Update = x.Features.ToMongoUpdate<IntegratedDocument, object>()
                };
            });
            var updateBatch = new MongoUpdateBatch<IntegratedDocument>(_documentStore, updateBatchSize);
            //BatchedBlockingBlock<FindAndModifyArgs<IntegratedDocument>>.CreateManyBlock(updateBatchSize);//
            featureBlock.LinkTo(updateCreator, new DataflowLinkOptions { PropagateCompletion = true });
            updateCreator.LinkTo(updateBatch.Block, new DataflowLinkOptions { PropagateCompletion = true });
            //updateBatch.LinkToEnd(new DataflowLinkOptions { PropagateCompletion = true});

            builder.AddCompletionTask(updateBatch.Block.Completion);
            harvester.SetDestination(builder);
            var results = await harvester.Run();
            Assert.True(results.ProcessedEntries == recordLimit);
        }

        private void DumpUsergroupSessionsToMongo(long userAPIId, EvalDictionaryBlock usersData)
        {
            var entityBlock = usersData;
            ApiAuth apiObject = _apiService.GetApi(userAPIId);
            var typeDef = DataIntegration.Factory.CreateFromType<DomainUserSessionCollection>("NetInfoUserSessions_7_8", apiObject);
            typeDef.AddField("is_paying", typeof(int));
            // _integrationService.SaveOrFetchExisting(ref typeDef);
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
                        IList<DomainUserSession> sessions = null;//NetinfoDonutfile.GetWebSessions(user).ToList();
                        var sessionWrapper = new DomainUserSessionCollection(sessions);
                        sessionWrapper.UserId = uuid;
                        sessionWrapper.Created = dateNoticed;

                        var document = IntegratedDocument.FromType<DomainUserSessionCollection, IntegratedDocument>(sessionWrapper, typeDef, userAPIId);
                        var documentBson = document.GetDocument();
                        documentBson["is_paying"] = userIsPaying ? 1 : 0;
                        document.IntegrationId = typeDef.Id;
                        //document.Save();
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\JoinedData" })]
        public async Task RestructureSheets(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var formatter = new CsvFormatter<ExpandoObject>() { Delimiter = ';' };
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, formatter);
            //formatter.FieldsToIgnore.Add("uuid");
            fileSource.Field("value")
                .AsString()
                .DuplicateAs("real_value");
            fileSource.Field("uuid")
                .EncodeWith<IdEncoding>()
                .DuplicateAs("real_uuid");
            fileSource.Field("paying")
                .SetValue(x => x["value"].ToString().Contains("payments/finish"));

            //_integrationService.ResolveFormatter<ExpandoObject>("text/csv");

            fileSource.SetFormatter(formatter);
            var ignName = "NetinfoJoinedData";
            DataImportTask<ExpandoObject> importTask = _integrationService.CreateIntegrationImportTask(fileSource, _appAuth, _user, ignName);
            var ign = DataIntegration.Wrap(importTask.Integration);
            _db.Integrations.Add(ign);
            _db.SaveChanges();
            importTask.Options.ShardLimit = 1;
            //importTask.Options.TotalEntryLimit = 100;
            var importResult = await importTask.Import();
            PipelineDefinition<BsonDocument, BsonDocument> pipelineWeeks = new BsonDocument[]
            {
                new BsonDocument
                {
                    {
                        "$group", new BsonDocument()
                        {
                            new BsonDocument("_id", new BsonDocument("$week", "$ondate")),
                            new BsonDocument("date", new BsonDocument("$first", "$ondate")),
                        }
                    }
                },
                new BsonDocument("$sort",  new BsonDocument("_id", 1))
            };
            AggregateOptions options = new AggregateOptions() { AllowDiskUse = true, BatchSize = 1000 };
            
            DateTime? firstTime = null;
            BsonDocument lastDoc = null;
            var csDoc = new CsvWriter($"{ignName}.csv");
            csDoc.WriteLine("id", "ondate", "event", "type", "value", "paid");
            HashSet<string> paidUserIds = new HashSet<string>();
            var col = importResult.Collection;
            var weeks = col.Aggregate(pipelineWeeks).ToList();
            bool passedWeek = false;
            foreach (var weekGroup in weeks)
            {
                var weekNum = weekGroup["_id"];
                var weekStart = weekGroup["date"].AsDateTime.StartOfWeek(DayOfWeek.Monday);
                var weekEnd = weekGroup["date"].AsDateTime.EndOfWeek(DayOfWeek.Monday);
                var nextWeekFilter = new BsonDocument { };
                nextWeekFilter["ondate"] = new BsonDocument { };
                nextWeekFilter["ondate"]["$gte"] = weekEnd;
                nextWeekFilter["ondate"]["$lt"] = weekEnd.AddDays(7);
                nextWeekFilter["paying"] = true;
                var nextWeekPayers = col.Distinct<string>("uuid", nextWeekFilter).ToList();
                if (nextWeekPayers.Count == 0) continue;

                var filter = new BsonDocument{ };
                filter["ondate"] = new BsonDocument { };
                filter["ondate"]["$gte"] = weekStart;
                filter["ondate"]["$lt"] = weekEnd;
                filter["paying"] = false;
                //filter["uuid"] = new BsonDocument { };
                //filter["uuid"]["$in"] = new BsonArray(nextWeekPayers);
                PipelineDefinition<BsonDocument, BsonDocument> pipeline = new BsonDocument[] {
                    new BsonDocument("$match", filter),
                    new BsonDocument("$sort", new BsonDocument("ondate", 1)) ,
                };

                var sortedEvents = col.Aggregate(pipeline, options)
                    .ToEnumerable();
                foreach (var ev in sortedEvents)
                {
                    passedWeek = true;
                    if (firstTime == null) firstTime = ev["ondate"].AsDateTime;
                    var isPaying = nextWeekPayers.Contains(ev["uuid"].ToString());
                    lastDoc = ev;
                    List<string> values = new List<string>();//uuid, ondate, event_id, type, real_value
                    values.Add(ev["uuid"].ToString());
                    values.Add(ev["ondate"].ToString());
                    values.Add(ev["event_id"].ToString());
                    values.Add(ev["type"].ToString());
                    values.Add(ev["real_value"].ToString());
                    values.Add(isPaying ? "1" : "0");
                    csDoc.WriteLine(values.ToArray());
                }
                if (passedWeek) break;
            }
            col.Drop();
        }





        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\JoinedData", "TestData\\Ebag\\demograpy.csv" })]
        public async void ExtractEntityFromDirectory(string inputDirectory, string demographySheet)
        {
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>() { Delimiter = ';' });

            int threadCount = 12;
            var harvester = new Harvester<ExpandoObject>(10);
            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var type = harvester.AddIntegrationSource(fileSource, apiObj, null);
            var cachedReducer = new ReduceCacheBlock(_appId, _redisConnection,
                (document) => $"{document.GetString("uuid")}:{document.GetDate("ondate")?.DaysTotal()}",
                (document) => document.Define("noticed_date", document.GetDate("ondate"))
                    .RemoveAll("event_id", "ondate", "value", "type"));
            var featureAccumulator = new FeatureGenerator<ExpandoObject>(threadCount);
            var featureAccBlock = featureAccumulator.CreateFeaturesBlock();
            //cachedReducer.LinkTo(featureAccBlock);
            cachedReducer.LinkTo(DataflowBlock.NullTarget<ExpandoObject>());


            //var helper = new CrossSiteAnalyticsHelper();//grouper.GetInputBlock());
            //var featureHelper = new NetinfoFeatureGeneratorHelper() { Helper = helper, TargetDomain = "ebag.bg" };
            var featureHelper = new NetinfoFeatureGeneratorHelper() { TargetDomain = "ebag.bg" };
            //            var featureGenerator = new FeatureGenerator<ExpandoObject>(featureHelper.GetFeatures, 12);
            //            featureGenerator.AddGenerator(featureHelper.GetAvgTimeBetweenSessionFeatures);
            //var featureGeneratorBlock = featureGenerator.CreateFeaturesBlock();

            var demographyImporter = new EntityDataImporter(
                demographySheet, true);
            //demographyImporter.SetEntityRelation((input, x) => input[0] == x.Document["uuid"]);
            demographyImporter.UseInputKey((input) => input[0]);
            demographyImporter.SetEntityKey((IntegratedDocument input) => input.GetString("uuid"));
            demographyImporter.JoinOn(JoinDemography);
            demographyImporter.ReadData();

            var insertCreator = new TransformBlock<FeaturesWrapper<ExpandoObject>, ExpandoObject>((x) =>
            {
                var doc = x.Document;
                //Todo: Fill doc with features 
                return doc;
            });
            //var insertBatcher = new MongoInsertBatch<ExpandoObject>(_documentStore, 3000);

            //demographyImporter.Helper = helper;
            //grouper.Helper = helper;
            //cachedReducer.LinkTo(featureGeneratorBlock);
            //demographyImporter.LinkTo(featureGeneratorBlock);
            //featureGeneratorBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            //insertCreator.LinkTo(insertBatcher.BatchBlock);

            //grouper.LinkOnComplete(demographyImporter);
            //cachedReducer.AddFlowCompletionTask(insertBatcher.Completion);

            harvester.AddIntegration(type, fileSource);
            harvester.SetDestination(cachedReducer);
            //var res1 = await harvester.ReadAll(grouper.GetInputBlock());
            var result = await harvester.Run();
            //            Console.ReadLine();
        }



        //        [Theory]
        //        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        //        public async void ExtractEventValueFeatures(string inputDirectory)
        //        {
        //            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
        //            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter() { Delimiter = ';' });
        //            var harvester = new Netlyt.Service.Harvester<IntegratedDocument>(_apiService, _integrationService, 10); 
        //            harvester.AddIntegrationSource(fileSource, _appId, null, true);
        //
        //            var grouper = new GroupingBlock(_appId,
        //                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.Day}",
        //                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
        //                (accumulator, newEntry) =>
        //                {
        //                    var newElement = new
        //                    {
        //                        ondate = newEntry.GetDate("ondate"),
        //                        event_id = newEntry.GetInt("event_id"),
        //                        type = newEntry.GetInt("type"),
        //                        value = newEntry.GetString("value")
        //                    }.ToBsonDocument();
        //                    accumulator.AddDocumentArrayItem("events", newElement);
        //                    return newElement;
        //                });
        //            grouper.Helper = _helper;
        //            grouper.LinkTo(new ActionBlock<IntegratedDocument>((x) =>
        //            {
        //                CollectTypeValuePair(x.GetString("uuid"), x.GetDocument());
        //            }));
        //            //Group the users
        //            // create features for each user -> create Update -> batch update 
        //            var updateCreator = new TransformBlock<FeaturesWrapper<IntegratedDocument>, FindAndModifyArgs<IntegratedDocument>>((x) =>
        //            {
        //                return new FindAndModifyArgs<IntegratedDocument>()
        //                {
        //                    Query = Builders<IntegratedDocument>.Filter.And(
        //                                Builders<IntegratedDocument>.Filter.Eq("Document.uuid", x.Document["uuid"].ToString()),
        //                                Builders<IntegratedDocument>.Filter.Eq("Document.noticed_date", x.Document.GetDate("noticed_date"))),
        //                    Update = x.Features.ToMongoUpdate<IntegratedDocument, object>()
        //                };
        //            });
        //            var updateBatcher = new MongoUpdateBatch<IntegratedDocument>(_documentStore, 300);
        //            var featuresBlock = featureGenerator.CreateFeaturesBlock();
        //            featuresBlock.LinkTo(updateCreator, new DataflowLinkOptions{ PropagateCompletion = true});
        //            updateCreator.LinkTo(updateBatcher.Block); 
        //            
        //            grouper.LinkOnCompleteEx(featuresBlock); 
        //
        //            harvester.SetDestination(grouper); 
        //            var completion = await harvester.Run();
        //            var syncDuration = harvester.ElapsedTime();
        //            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
        //
        //        } 

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
                //_helper.AddRatingFeature(VisitTypedValue, userId, key_value); 
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
            if (value.Contains("payments/finish") && Donut.Blocks.Extensions.ToHostname(value).Contains("ebag.bg"))
            {
                accumulator["is_paying"] = 1;
            }
            return newEntry;
        }


        private object AccumulateUserDocument(IIntegratedDocument accumulator, BsonDocument newEntry, bool appendEvent = true)
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
            var pageHost = Donut.Blocks.Extensions.ToHostname(value);
            var pageSelector = pageHost;
            //            if (!_helper.Stats.ContainsPage(pageHost))
            //            {
            //                _helper.Stats.AddPage(pageSelector, new PageStats()
            //                {
            //                    Page = value
            //                }); 
            //            }
            //            _helper.Stats[pageSelector].PageVisitsTotal++;

            if (appendEvent)
            {
                accumulator.AddDocumentArrayItem("events", newEntry);
            }
            if (value.Contains("payments/finish") && Donut.Blocks.Extensions.ToHostname(value).Contains("ebag.bg"))
            {
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
                accumulator["is_paying"] = 1;
            }
            return newEntry;
        }

    }
}
