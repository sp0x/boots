using System;
using System.Linq;
using System.Threading.Tasks;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Integration.Import;
using Netlyt.Service.Orion;
using Netlyt.Service.Source;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.FeatureGeneration
{
    [Collection("DonutTests")]
    public class OneHotEvalTests
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

        public OneHotEvalTests(DonutConfigurationFixture fixture)
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
        }

        [Theory]
        [InlineData("Res\\TestData\\pollution_data.csv")]
        public async Task TestOneHotForStrings(string sourceFile)
        {
            //Source
            var modelName = "FunModel";
            var integrationResult = await _integrationService.CreateOrAppendToIntegration(sourceFile, _appAuth, _user, modelName);
            var newIntegration = integrationResult?.Integration;
            //Assert the category field has a DataEncoding of OneHot
            var categoryField = newIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            Assert.NotNull(categoryField);
            Assert.Equal(FieldDataEncoding.OneHot,
                categoryField.DataEncoding);
        }

        [Theory]
        [InlineData("Res\\TestData\\pollution_data.csv")]
        public async Task TestOneHotEvalOnIntegration(string sourceFile)
        {
            //Source
            var modelName = "FunModel";
            var integrationResult = await _integrationService.CreateOrAppendToIntegration(sourceFile, _appAuth, _user, modelName);
            var newIntegration = integrationResult?.Integration;
            OneHotEncodeTaskOptions options = new OneHotEncodeTaskOptions
            {
                Integration = newIntegration
            };
            var ht = new OneHotEncodeTask(options);
            var encodedIntegration = ht.GetEncodedIntegration();
            //Assert the category field has a DataEncoding of OneHot
            var categoryField = encodedIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            Assert.True(categoryField.Extras.Extra.Count == 2);
            _db.Integrations.Add(newIntegration);
            _db.SaveChanges();
            var result = await ht.ApplyToField(categoryField);
            Assert.Equal(5402, result.ModifiedCount);
            Assert.Equal(5402, result.MatchedCount);
            Assert.Equal(2, result.ProcessedRequests.Count);
            Assert.Equal(0, result.Upserts.Count);
            
            //Cleanup
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var dstCollection = new MongoList(databaseConfiguration, newIntegration.Collection);
            dstCollection.Trash();
        }

        [Theory]
        [InlineData("Res\\TestData\\pollution_data.csv")]
        public async Task TestOneHotEvalOnAppend(string sourceFile)
        {
            //Source
            var modelName = "FunModel";
            var integrationResult = await _integrationService.CreateOrAppendToIntegration(sourceFile, _appAuth, _user, modelName);
            var newIntegration = integrationResult?.Integration;
            OneHotEncodeTaskOptions options = new OneHotEncodeTaskOptions
            {
                Integration = newIntegration
            };
            var ht = new OneHotEncodeTask(options);
            //Get our encoding
            var encodedIntegration = ht.GetEncodedIntegration();
            //Assert the category field has a DataEncoding of OneHot
            var categoryField = encodedIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            _db.Integrations.Add(newIntegration);
            _db.SaveChanges();

            Assert.True(categoryField.Extras.Extra.Count == 2);
            //Apply it
            var result = await ht.ApplyToField(categoryField);

            Assert.Equal(1, result.ModifiedCount);
            Assert.Equal(5402, result.MatchedCount);
            Assert.Equal(2, result.ProcessedRequests.Count);
            Assert.Equal(0, result.Upserts.Count);
            var addResult = await _integrationService.AppendToIntegration(newIntegration, sourceFile, _appAuth);
            Assert.Equal(5402, addResult.Data.ProcessedEntries);
            //Cleanup
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var dstCollection = new MongoList(databaseConfiguration, newIntegration.Collection);
            Assert.Equal(10804, dstCollection.Size);
            dstCollection.Trash();
        }
    }
}