using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Donut;
using Donut.Caching;
using Donut.FeatureGeneration;
using Donut.Features;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Donut.Lex.Generators;
using Donut.Models;
using Donut.Orion;
using Donut.Source;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Models;
using Netlyt.ServiceTests.Fixtures;
using Newtonsoft.Json.Linq;
using Xunit;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.ServiceTests.FeatureGeneration
{
    [Collection("DonutTests")]
    public class FeatureGenerationTests
    {
        private CompilerService _compiler;
        private ApiService _apiService;
        private IIntegrationService _integrationService;
        private ApiAuth _appAuth;
        private IServiceProvider _serviceProvider;
        private IDatabaseConfiguration _dbConfig;
        private IRedisCacher _cacher;
        private ManagementDbContext _db;
        private UserService _userService;
        private ModelService _modelService;
        private User _user;
        private IOrionContext _orion;
        private TimestampService _timestampService;
        private DonutConfigurationFixture _fixture;

        public FeatureGenerationTests(DonutConfigurationFixture fixture)
        {
            _compiler = fixture.GetService<CompilerService>();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IIntegrationService>();
            _userService = fixture.GetService<UserService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _db = fixture.GetService<ManagementDbContext>();
            _orion = fixture.GetService<OrionContext>();
//            var orionCtx = new Mock<IOrionContext>();
//            orionCtx.Setup(m => m.Query(It.IsAny<OrionQuery>())).ReturnsAsync(new JObject { {"id", 1} });
//            _orion = orionCtx.Object;

            _user = _userService.GetByApiKey(_appAuth);
            if (_user == null)
            {
                _user = new User() { FirstName = "tester1231", Email = "mail@lol.co" };
                _userService.CreateUser(_user, "Password-IsStrong!", _appAuth);
            }
            _cacher = fixture.GetService<IRedisCacher>();
            _serviceProvider = fixture.ServiceProvider;//GetService<IServiceProvider>();
            _dbConfig = (DBConfig.GetInstance().GetGeneralDatabase()).ToDonutDbConfig();
            _modelService = fixture.GetService<ModelService>();
            _timestampService = new TimestampService(_db);
            _fixture = fixture;
        }

        

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public async Task CreateFeatureGenerationRequest()
        { 
            var newModel = await _fixture.GetModel(_appAuth);
            var targetAttribute = "pm10";
            var rootIgn = newModel.GetRootIntegration();
            var fldCategory = rootIgn.AddField<string>("category", FieldDataEncoding.BinaryIntId);
            fldCategory.Extras = new FieldExtras();
            fldCategory.Extras.Extra.Add(new FieldExtra("100000001", "aba"));
            fldCategory.Extras.Extra.Add(new FieldExtra("100000010", "acaba"));
            var fldEvent = rootIgn.AddField<string>("event", FieldDataEncoding.OneHot);
            fldEvent.Extras = new FieldExtras();
            fldEvent.Extras.Extra.Add(new FieldExtra("event1", "visit"));
            fldEvent.Extras.Extra.Add(new FieldExtra("event2", "stay"));
            var collections = newModel.GetFeatureGenerationCollections(targetAttribute);
            var query = OrionQuery.Factory.CreateFeatureDefinitionGenerationQuery(newModel, collections, null, targetAttribute);
            //var queryResult = await _orion.Query(query);
            var jsQuery = query.Serialize();
            var jsFields = jsQuery["params"]["collections"][0]["fields"];
            Assert.Equal(12, jsFields.Count());

        }

        [Fact]
        public async Task TrainGeneratedFeatures()
        {
            Model model = await _fixture.GetModel(_appAuth);
            DataIntegration ign = model.GetRootIntegration();
            var query = OrionQuery.Factory.CreateTrainQuery(model, ign);
            //var payload = query.Serialize();
            Assert.True(query.Operation == OrionOp.Train);
        }



        [Fact]
        public async Task IntegrationTest()
        {
            string dataSource = "Romanian";
            var integration = _userService.GetUserIntegration(_user, dataSource);
            var relations = new List<FeatureGenerationRelation>();
            //            for (var i = 0; i < 1; i++)
            //            {
            //                var nr = new FeatureGenerationRelation("Events.uuid", "Demography.uuid");
            //                relations.Add(nr);
            //            }
            string modelName = "Rommol1";
            string modelCallback = "http://localhost:9999";
            string targetAttribute = "pm10"; //"Events.is_paying";
            //item.Relations?.Select(x => new FeatureGenerationRelation(x[0], x[1]));
            //This really needs a builder..
            var newModel = await _modelService.CreateModel(_user,
                modelName,
                new List<DataIntegration>(new[] { integration }),
                modelCallback,
                true,
                relations,
                targetAttribute);
            Assert.NotNull(newModel);

            Console.ReadLine();//We hang on here, waiting for the features to be generated ..
        }


        [Fact]
        public async Task TestGeneratedUnderscoreFeatures()
        {
            var model = await _fixture.GetModel(_appAuth);
            var features = @"MIN(Romanian.rssi)
DAY(first_Romanian_time)
YEAR(first_Romanian_time)
MONTH(first_Romanian_time)
WEEKDAY(first_Romanian_time)";
            string[] featureBodies = features.Split('\n');
            string donutName = $"{model.ModelName}Donut";
            DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, "pm10", model.GetRootIntegration(), featureBodies);
            foreach (var modelIgn in model.DataIntegrations)
            {
                dscript.AddIntegrations(modelIgn.Integration);
            }
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, model.ModelName, out donutType, out donutContextType, out donutFEmitterType);
            Assert.NotNull(assembly);
        }

        [Fact]
        public async Task TestGeneratedNestedFeatures()
        {
            var model = await _fixture.GetModel(_appAuth);
            string features = @"MIN(Romanian.WEEKDAY(timestamp))";
            string[] featureBodies = features.Split('\n');
            string donutName = $"{model.ModelName}Donut";
            DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, "pm10", model.GetRootIntegration(), featureBodies);
            foreach (var modelIgn in model.DataIntegrations)
            {
                dscript.AddIntegrations(modelIgn.Integration);
            }
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, model.ModelName, out donutType, out donutContextType, out donutFEmitterType);
            Assert.NotNull(assembly);
            model.DonutScript = new DonutScriptInfo(dscript);
            model.DonutScript.AssemblyPath = assembly.Location;
            model.DonutScript.Model = model;
            _db.SaveChanges();
        }
        [Fact]
        public async Task TestGeneratedNestedDotFeatures()
        {
            var model = await _fixture.GetModel(_appAuth, "feature_test.csv", "feature_test.csv");
            string features = @"SUM(feature_test.csv.humidity)";
            string[] featureBodies = features.Split('\n');
            string donutName = $"{model.ModelName}Donut";
            DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, "pm10", model.GetRootIntegration(), featureBodies);
            foreach (var modelIgn in model.DataIntegrations)
            {
                dscript.AddIntegrations(modelIgn.Integration);
            }
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, model.ModelName, out donutType, out donutContextType, out donutFEmitterType);
            Assert.NotNull(assembly);
            model.DonutScript = new DonutScriptInfo(dscript);
            model.DonutScript.AssemblyPath = assembly.Location;
            model.DonutScript.Model = model;
            _db.SaveChanges();
            //SUM(feature_test.csv.humidity)
        }

        [Theory]
        [InlineData("max(pm10)", "{ \"f_0\" : { \"$max\" : \"$pm10\" } }")]
        [InlineData("min(pm10)", "{ \"f_0\" : { \"$min\" : \"$pm10\" } }")]
        [InlineData("sum(pm10)", "{ \"f_0\" : { \"$sum\" : \"$pm10\" } }")]
        [InlineData("std(pm10)", "{ \"f_0\" : { \"$stdDevSamp\" : \"$pm10\" } }")]
        [InlineData("mean(pm10)", "{ \"f_0\" : { \"$avg\" : \"$pm10\" } }")]
        [InlineData("avg(pm10)", "{ \"f_0\" : { \"$avg\" : \"$pm10\" } }")]
        [InlineData("SUM(feature_test.csv.humidity)", "{ \"f_0\" : { \"$sum\" : \"$humidity\" } }")]
        public void TestDonutAggregateFunction(string featureBody, string expectedAggregateValue)
        {
            var ign = new DataIntegration("feature_test.csv", true);
            var tsField = new FieldDefinition("timestamp", typeof(DateTime));
            var pm10Field = new FieldDefinition("pm10", typeof(Double));
            ign.Fields.Add(tsField);
            ign.Fields.Add(pm10Field);
            var script = DonutScript.Factory.CreateWithFeatures("SomeDonut", "pm10", ign, featureBody);
            var parser = new DonutScriptCodeGenerator(null);
            var firstFeature = script.Features.FirstOrDefault();
            var maxCall = firstFeature?.Value as CallExpression;
            var faggr = new AggregateFeatureCodeGenerator(script, new AggregateFeatureGeneratingExpressionVisitor(script));
            var maxFeature = faggr.GenerateFeatureFunctionCall(maxCall, firstFeature);
            Assert.Equal(expectedAggregateValue, maxFeature.GetValue());

            var query = BsonDocument.Parse(maxFeature.GetValue());
            query["_id"] = "$_id";
            //var result = collection.Aggregate().Group(query).ToList();
            //Assert.Equal(100, result.Count);
        }

        [Theory]
        [InlineData("day(timestamp)", "{ \"f_0\" : { \"$dayOfMonth\" : \"$timestamp\" } }")]
        [InlineData("year(timestamp)", "{ \"f_0\" : { \"$year\" : \"$timestamp\" } }")]
        [InlineData("month(timestamp)", "{ \"f_0\" : { \"$month\" : \"$timestamp\" } }")]
        //[InlineData("SUM(feature_test.csv.humidity)", "{ \"f_0\" : { \"$month\" : \"$timestamp\" } }")]
        public void TestDonutTimeAggregateFunction(string featureBody, string expectedAggregateValue)
        {
            var collection = GetTestingCollection();
            //            var validProjection = new BsonDocument();
            //            validProjection[ "_id"] = "$_id";
            //            validProjection[fieldName + "_v"] = new BsonDocument {{ "$max", "$" + fieldName }};
            //            var validResult = collection.Aggregate().Group(validProjection).ToList();
            var ign = new DataIntegration("feature_test.csv", true);
            var tsField = new FieldDefinition("timestamp", typeof(DateTime));
            var hdField = new FieldDefinition("humidity", typeof(Double));
            var pm10Field = new FieldDefinition("pm10", typeof(Double));
            ign.Fields.Add(tsField);
            ign.Fields.Add(hdField);
            ign.Fields.Add(pm10Field);
            var script = DonutScript.Factory.CreateWithFeatures("SomeDonut", "pm10", ign, featureBody);
            var parser = new DonutScriptCodeGenerator(null);
            var firstFeature = script.Features.FirstOrDefault();
            var maxCall = firstFeature?.Value as CallExpression;
            var faggr = new AggregateFeatureCodeGenerator(script, null);
            var maxFeatureStr = faggr.GenerateFeatureFunctionCall(maxCall, firstFeature);
            //var groupings = parser.GetAggregates(DonutFunctionType.Group);
            //var projections = parser.GetAggregates(DonutFunctionType.Project);
            Assert.Equal(expectedAggregateValue, maxFeatureStr.GetValue());

            var projectionsDoc = BsonDocument.Parse(maxFeatureStr.Projections);
            var result = collection.Aggregate().Project(projectionsDoc).ToList();
            Assert.Equal(100, result.Count);
        }

        [Theory]
        [InlineData("NUM_UNIQUE(Romanian.DAY(timestamp))", "{ \"f_0\" : { \"$max\" : \"$someint\" } }")]
        public void TestDonutNestedAggregateFunction(string featureBody, string expectedAggregateValue)
        {
            var collection = GetTestingCollection();
            //            var validProjection = new BsonDocument();
            //            validProjection[ "_id"] = "$_id";
            //            validProjection[fieldName + "_v"] = new BsonDocument {{ "$max", "$" + fieldName }};
            //            var validResult = collection.Aggregate().Group(validProjection).ToList();
            var ign = new DataIntegration() { Name = "Romanian" };
            var tsField = new FieldDefinition("timestamp", typeof(DateTime));
            var pm10Field = new FieldDefinition("pm10", typeof(Double));
            ign.Fields.Add(tsField);
            ign.Fields.Add(pm10Field);
            var script = DonutScript.Factory.CreateWithFeatures("SomeDonut", "pm10", ign, featureBody);

            script.Integrations.Add(ign);
            var parser = new DonutScriptCodeGenerator(script);
            var firstFeature = script.Features.FirstOrDefault();
            var maxCall = firstFeature?.Value as CallExpression;
            var faggr = new AggregateFeatureCodeGenerator(script, null);
            var maxFeatureStr = faggr.GenerateFeatureFunctionCall(maxCall, firstFeature);
            Assert.Equal(expectedAggregateValue, maxFeatureStr.GetValue());

            var query = BsonDocument.Parse(maxFeatureStr.GetValue());
            query["_id"] = "$_id";
            var result = collection.Aggregate().Group(query).ToList();
            Assert.Equal(100, result.Count);
        }


        private IMongoCollection<BsonDocument> GetTestingCollection()
        {
            var mongoList = new MongoList(_dbConfig.Name, "_testing_collection", _dbConfig.GetUrl());
            mongoList.Truncate();
            IMongoCollection<BsonDocument> collection = mongoList.Records;
            int i = 0;
            FillCollection(collection, () => new BsonDocument
            {
                {"someint", i++ },
                {"timestamp", DateTime.Today.AddDays(i) }
            }, 100);
            return collection;
        }

        private void FillCollection(IMongoCollection<BsonDocument> collection, Func<BsonDocument> generator, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var doc = generator();
                collection.InsertOne(doc);
            }
        }
    }
}

