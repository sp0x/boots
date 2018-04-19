﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generators;
using Netlyt.Service.Ml;
using Netlyt.Service.Orion;
using Netlyt.Service.Source;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.FeatureGeneration
{
    [Collection("DonutTests")]
    public class FeatureGenerationTests
    {
        private CompilerService _compiler;
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private ApiAuth _appAuth;
        private IServiceProvider _serviceProvider;
        private DatabaseConfiguration _dbConfig;
        private RedisCacher _cacher;
        private ManagementDbContext _db;
        private UserService _userService;
        private ModelService _modelService;
        private User _user;
        private OrionContext _orion;
        private TimestampService _timestampService;

        public FeatureGenerationTests(DonutConfigurationFixture fixture)
        {
            _compiler = fixture.GetService<CompilerService>();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _userService = fixture.GetService<UserService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _db = fixture.GetService<ManagementDbContext>();
            _orion = fixture.GetService<OrionContext>();
            _user = _userService.GetByApiKey(_appAuth);
            if (_user == null)
            {
                _user = new User() { FirstName = "tester1231", Email = "mail@lol.co" };
                _userService.CreateUser(_user, "Password-IsStrong!", _appAuth);
            }
            _cacher = fixture.GetService<RedisCacher>();
            _dbConfig = DBConfig.GetGeneralDatabase();
            _serviceProvider = fixture.GetService<IServiceProvider>();
            _modelService = fixture.GetService<ModelService>();
            _timestampService = new TimestampService(_db);
        }

        private Model GetModel(string modelName = "Romanian", string integrationName = "Namex")
        {
            var ignName = modelName;//Must match the features
            var model = new Model()
            {
                ModelName = modelName
            };//_db.Models.Include(x=>x.DataIntegrations).FirstOrDefault(x => x.Id == modelId);
            var rootIntegration = new DataIntegration(integrationName, true)
            {
                APIKey = _appAuth,
                APIKeyId = _appAuth.Id,
                Name = ignName,
                DataTimestampColumn = "timestamp",
            };
            rootIntegration.AddField<double>("humidity");
            rootIntegration.AddField<double>("latitude");
            rootIntegration.AddField<double>("longitude");
            rootIntegration.AddField<double>("pm10");
            rootIntegration.AddField<double>("pm25");
            rootIntegration.AddField<double>("pressure");
            rootIntegration.AddField<double>("rssi");
            rootIntegration.AddField<double>("temperature");
            rootIntegration.AddField<DateTime>("timestamp");
            var modelIntegration = new ModelIntegration() { Model = model, Integration = rootIntegration };
            model.DataIntegrations.Add(modelIntegration);
            return model;
        }

        [Fact]
        public void CreateFeatureGenerationRequest()
        { 
            var newModel = GetModel();
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
            var query = OrionQuery.Factory.CreateFeatureGenerationQuery(newModel, collections, null, targetAttribute);
            var jsQuery = query.Serialize();
            var jsFields = jsQuery["params"]["collections"][0]["fields"];
            Assert.Equal(12, jsFields.Count());
        }

        [Fact]
        public async Task TrainGeneratedFeatures()
        {
            Model model = _db.Models
                .Include(x => x.DataIntegrations)
                .Include(x => x.User)
                .FirstOrDefault(x => x.Id == 224);
            DataIntegration ign = _db.Integrations.FirstOrDefault(x => x.Id == model.DataIntegrations.FirstOrDefault().IntegrationId);
            var query = OrionQuery.Factory.CreateTrainQuery(model, ign);
            var m_id = await _orion.Query(query);
            m_id = m_id;
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
            var model = GetModel();
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
            var model = GetModel();
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
            var model = GetModel("feature_test.csv", "feature_test.csv");
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
            var faggr = new FeatureAggregateCodeGenerator(script, new DonutFeatureGeneratingExpressionVisitor(script));
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
            var faggr = new FeatureAggregateCodeGenerator(script, null);
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
            var faggr = new FeatureAggregateCodeGenerator(script, null);
            var maxFeatureStr = faggr.GenerateFeatureFunctionCall(maxCall, firstFeature);
            Assert.Equal(expectedAggregateValue, maxFeatureStr.GetValue());

            var query = BsonDocument.Parse(maxFeatureStr.GetValue());
            query["_id"] = "$_id";
            var result = collection.Aggregate().Group(query).ToList();
            Assert.Equal(100, result.Count);
        }


        private IMongoCollection<BsonDocument> GetTestingCollection()
        {
            var mongoList = new MongoList(_dbConfig, "_testing_collection");
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

