using System;
using System.Linq;
using System.Threading.Tasks;
using Donut;
using Donut.Encoding;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.ServiceTests.Fixtures;
using Xunit;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.ServiceTests.FeatureGeneration
{
    [Collection("DonutTests")]
    public class OneHotEvalTests
    {
        private ApiService _apiService;
        private IIntegrationService _integrationService;
        private ApiAuth _appAuth;
        private ManagementDbContext _db;
        private UserService _userService;
        private User _user;

        public OneHotEvalTests(DonutConfigurationFixture fixture)
        {
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IIntegrationService>();
            _userService = fixture.GetService<UserService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _db = fixture.GetService<ManagementDbContext>();
            _user = _userService.GetByApiKey(_appAuth);
            if (_user == null)
            {
                _user = new User() { FirstName = "tester1231", Email = "mail@lol.co" };
                _userService.CreateUser(_user, "Password-IsStrong!", _appAuth);
            }
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
            var newIntegration = DataIntegration.Wrap(integrationResult?.Integration);
            FieldEncodingOptions options = new FieldEncodingOptions
            {
                Integration = newIntegration
            };
            var ht = new OneHotEncoding(options);
            var encodedIntegration = ht.GetEncodedIntegration();
            //Assert the category field has a DataEncoding of OneHot
            var categoryField = encodedIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            Assert.True(categoryField.Extras.Extra.Count == 2);
            _db.Integrations.Add(newIntegration);
            _db.SaveChanges();
            var dbc = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            var result = await ht.ApplyToField(categoryField, MongoHelper.GetCollection(dbc, newIntegration.Collection));
            Assert.Equal(5402, result.ModifiedCount);
            Assert.Equal(5402, result.MatchedCount);
            Assert.Equal(2, result.ProcessedRequests.Count);
            Assert.Equal(0, result.Upserts.Count);
            
            //Cleanup
            var dstCollection = new MongoList(dbc.Name, newIntegration.Collection, dbc.GetUrl());
            dstCollection.Trash();
        }

        [Theory]
        [InlineData("Res\\TestData\\pollution_data.csv")]
        public async Task TestOneHotEvalOnAppend(string sourceFile)
        {
            //Source
            var modelName = "FunModel";
            var integrationResult = await _integrationService.CreateOrAppendToIntegration(sourceFile, _appAuth, _user, modelName);
            var newIntegration = DataIntegration.Wrap(integrationResult?.Integration);
            FieldEncodingOptions options = new FieldEncodingOptions
            {
                Integration = newIntegration
            };
            var ht = new OneHotEncoding(options);
            //Get our encoding
            var encodedIntegration = ht.GetEncodedIntegration();
            //Assert the category field has a DataEncoding of OneHot
            var categoryField = encodedIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            _db.Integrations.Add(newIntegration);
            _db.SaveChanges();

            Assert.True(categoryField.Extras.Extra.Count == 2);
            var dbc = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            //Apply it
            var result = await ht.ApplyToField(categoryField, MongoHelper.GetCollection(dbc, newIntegration.Collection));

            Assert.Equal(1, result.ModifiedCount);
            Assert.Equal(5402, result.MatchedCount);
            Assert.Equal(2, result.ProcessedRequests.Count);
            Assert.Equal(0, result.Upserts.Count);
            var addResult = await _integrationService.AppendToIntegration(newIntegration, sourceFile, _appAuth);
            Assert.Equal(5402, addResult.Data.ProcessedEntries);
            //Cleanup
            
            var dstCollection = new MongoList(dbc.Name, newIntegration.Collection, dbc.GetUrl());
            Assert.Equal(10804, dstCollection.Size);
            dstCollection.Trash();
        }
    }
}