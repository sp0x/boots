using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Donut;
using Donut.Caching;
using Donut.Orion;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using Netlyt.Service.Models;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.IntegrationTests
{
    [Collection("DonutTests")]
    public class OrionFeatureGenerationTests
    {
        private CompilerService _compiler;
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private ApiAuth _appAuth;
        private IServiceProvider _serviceProvider;
        private IDatabaseConfiguration _dbConfig;
        private RedisCacher _cacher;
        private ManagementDbContext _db;
        private UserService _userService;
        private ModelService _modelService;
        private User _user;
        private IOrionContext _orion;
        private TimestampService _timestampService;
        private DonutOrionHandler _orionHandler;

        public OrionFeatureGenerationTests(DonutConfigurationFixture fixture)
        {
            _compiler = fixture.GetService<CompilerService>();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
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
            _cacher = fixture.GetService<RedisCacher>();
            _dbConfig = new NetlytDbConfig(DBConfig.GetInstance().GetGeneralDatabase());
            _serviceProvider = fixture.GetService<IServiceProvider>();
            _modelService = fixture.GetService<ModelService>();
            _timestampService = new TimestampService(_db);
            _orionHandler = fixture.GetService<DonutOrionHandler>();
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
            var model = Utils.GetModel(_appAuth, modelName, newIntegration);

            _db.Models.Add(model);
            var collections = model.GetFeatureGenerationCollections(targetAttribute);
            //Assert the category field has a DataEncoding of BinaryIntId
            var categoryField = newIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            Assert.NotNull(categoryField);
            Assert.Equal(FieldDataEncoding.BinaryIntId,
                categoryField.DataEncoding);
            _db.SaveChanges();
            var query = OrionQuery.Factory.CreateFeatureGenerationQuery(model, collections, null, targetAttribute);
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
