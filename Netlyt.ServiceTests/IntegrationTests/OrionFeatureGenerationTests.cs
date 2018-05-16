using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Donut;
using Donut.Caching;
using Donut.Data;
using Donut.FeatureGeneration;
using Donut.IntegrationSource;
using Donut.Lex.Data;
using Donut.Orion;
using MongoDB.Bson;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using Netlyt.Service.Models;
using Netlyt.ServiceTests.Fixtures;
using Romanian;
using Xunit;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.ServiceTests.IntegrationTests
{
    [Collection("DonutTests")]
    public class OrionFeatureGenerationTests
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
        private DonutOrionHandler _orionHandler;
        private DonutConfigurationFixture _fixture;

        public OrionFeatureGenerationTests(DonutConfigurationFixture fixture)
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
            _dbConfig = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            _serviceProvider = fixture.GetService<IServiceProvider>();
            _modelService = fixture.GetService<ModelService>();
            _timestampService = new TimestampService(_db);
            _orionHandler = fixture.GetService<DonutOrionHandler>();
            _fixture = fixture;
        }



        [Theory]
        [InlineData(new object[]{ "Res\\TestData\\pollution_data.csv" })]
        public async Task TestMongoDonutJointFeatures(string sourceFile)
        {
            var modelName = "Romanian";
            IInputSource inputSource;
            var integration = _fixture.GetIntegrationByFile(sourceFile, modelName, _appAuth, _user, out inputSource);
            var model = await _fixture.GetModel(_appAuth, modelName, integration);
            //Run file integration
            var features = @"MIN(Romanian.rssi)
NUM_UNIQUE(Romanian.pm25)";
            string[] featureBodies = features.Split('\n');
            string donutName = $"{model.ModelName}Donut";
            DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, "pm10", model.GetRootIntegration(), featureBodies);
            foreach (var modelIgn in model.DataIntegrations)
            {
                dscript.AddIntegrations(modelIgn.Integration);
            }
            Type donutType, donutContextType, donutFEmitterType;
//            var assembly = _compiler.Compile(dscript, model.ModelName, out donutType, out donutContextType, out donutFEmitterType);
//            Assert.NotNull(assembly);
            donutType = typeof(RomanianDonut);
            donutContextType = typeof(RomanianDonutContext);
            donutFEmitterType = typeof(RomanianFeatureGenerator);

            //Create a donut and a donutRunner
            var donutMachine = DonutGeneratorFactory.Create<IntegratedDocument>(donutType, donutContextType, integration, _cacher, _serviceProvider);
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(10000);
            donut.ReplayInputOnFeatures = true;
            var harvester = new Harvester<IntegratedDocument>(10);
            harvester.AddIntegration(integration, inputSource);
            
            IDonutRunner<IntegratedDocument> donutRunner = DonutRunnerFactory.CreateByType(donutType, donutContextType, harvester, _dbConfig, integration.FeaturesCollection);

            var featureGenerator = FeatureGeneratorFactory<IntegratedDocument>.Create(donut, donutFEmitterType);

            var result = await donutRunner.Run(donut, featureGenerator);
            integration.GetMongoFeaturesCollection<BsonDocument>().Drop();
            integration.GetMongoCollection<BsonDocument>().Drop();
        }


        [Theory]
        [InlineData("Res\\TestData\\pollution_data.csv")]
        public async Task TestOneHotForStrings(string sourceFile)
        {
            //Source
            var modelName = "FunModel";
            var targetAttribute = "pm10";
            var integrationResult = await _integrationService.CreateOrAppendToIntegration(sourceFile, _appAuth, _user, modelName);
            var newIntegration = DataIntegration.Wrap(integrationResult?.Integration);
            var model = await _fixture.GetModel(_appAuth, modelName, newIntegration);

            _db.Models.Add(model);
            var collections = model.GetFeatureGenerationCollections(targetAttribute);
            //Assert the category field has a DataEncoding of BinaryIntId
            var categoryField = newIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            Assert.NotNull(categoryField);
            Assert.Equal(FieldDataEncoding.BinaryIntId,
                categoryField.DataEncoding);
            _db.SaveChanges();
            var query = OrionQuery.Factory.CreateFeatureDefinitionGenerationQuery(model, collections, null, targetAttribute);
            var featuresAwaiter = new SemaphoreSlim(0, 1);
            _orionHandler.ModelFeaturesGenerated += (sender, updatedModel) =>
            {
                featuresAwaiter.Release();
                model = updatedModel;
            };
            var queryResult = await _orion.Query(query);
            await featuresAwaiter.WaitAsync(); //Wait for the semaphore
            Assert.NotNull(model.DonutScript);
            //Cleanup
            new MongoList(_dbConfig.Name, newIntegration.Collection, _dbConfig.GetUrl()).Truncate();
        }
        
    }
}
