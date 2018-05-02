using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Donut;
using Donut.Encoding;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.FeatureGeneration
{
    [Collection("Data Sources")]
    public class BinaryCategoryEncodeTests
    {
        private ApiService _apiService;
        private IIntegrationService _integrationService;
        private ApiAuth _appAuth;
        private ManagementDbContext _db;
        private UserService _userService;
        private User _user;
        private IDatabaseConfiguration _dbConfig;

        public BinaryCategoryEncodeTests(ConfigurationFixture fixture)
        {
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IIntegrationService>();
            _userService = fixture.GetService<UserService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null)
            {
                _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1"); 
            }
            _db = fixture.GetService<ManagementDbContext>();
            _dbConfig = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            _user = _userService.GetByApiKey(_appAuth);
            if (_user == null)
            {
                _user = new User() { FirstName = "tester1231", Email = "mail@lol.co" };
                _userService.CreateUser(_user, "Password-IsStrong!", _appAuth);
            }
        }

        [Theory]
        [InlineData("Res\\TestData\\pollution_data.csv")]
        public async Task TestForStrings(string sourceFile)
        {
            //Source
            var modelName = "FunModel";
            var integrationResult = await _integrationService.CreateOrAppendToIntegration(sourceFile, _appAuth, _user, modelName);
            var newIntegration = integrationResult?.Integration;
            //Assert the category field has a DataEncoding of OneHot
            var categoryField = newIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            Assert.NotNull(categoryField);
            Assert.Equal(FieldDataEncoding.BinaryIntId,
                categoryField.DataEncoding);
            //Cleanup
            var dbc = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            var dstCollection = new MongoList(dbc.Name, newIntegration.Collection, dbc.GetUrl());
            dstCollection.Trash();
        }

        [Theory]
        [InlineData("Res\\TestData\\pollution_data.csv")]
        public async Task TestOnIntegration(string sourceFile)
        {
            //Source
            var modelName = "FunModel";
            var integrationResult = await _integrationService.CreateOrAppendToIntegration(sourceFile, _appAuth, _user, modelName);
            var newIntegration = DataIntegration.Wrap(integrationResult?.Integration);
            FieldEncodingOptions options = new FieldEncodingOptions
            {
                Integration = newIntegration
            };
            var ht = new BinaryCategoryEncoding(options);
            var encodedIntegration = ht.GetEncodedIntegration();
            //Assert the category field has a DataEncoding of OneHot
            var categoryField = encodedIntegration.Fields.FirstOrDefault(x => x.Name == "category");
            Assert.True(categoryField.Extras.Extra.Count == 2);
            if(newIntegration.Id==0)_db.Integrations.Add(newIntegration);
            _db.SaveChanges();
            var db = MongoHelper.GetCollection(_dbConfig, newIntegration.Name);
            var result = await ht.ApplyToField(categoryField, db);
            Assert.Equal(0, result.ModifiedCount);
            Assert.Equal(0, result.MatchedCount);
            Assert.Equal(2, result.ProcessedRequests.Count);
            Assert.Equal(0, result.Upserts.Count);

            //Cleanup
            var dbc = (DBConfig.GetInstance().GetGeneralDatabase()).ToDonutDbConfig();
            var dstCollection = new MongoList(dbc.Name, newIntegration.Collection, dbc.GetUrl());
            dstCollection.Trash();
        }

        [Theory]
        [InlineData("Res\\TestData\\pollution_data.csv")]
        public async Task TestOnAppend(string sourceFile)
        {
            //Source
            var modelName = "FunModel";
            var integrationTask = _integrationService.CreateIntegrationImportTask(sourceFile, _appAuth, _user, modelName);
            //integrationTask.EncodeOnImport = false;
            await integrationTask.Import();
            var newIntegration = DataIntegration.Wrap(integrationTask?.Integration);
            FieldEncodingOptions options = new FieldEncodingOptions
            {
                Integration = newIntegration
            };
            var ht = new BinaryCategoryEncoding(options);
            //Get our encoding
            var encodedIntegration = ht.GetEncodedIntegration();
            //Assert the category field has a DataEncoding of OneHot
            var categoryField = encodedIntegration.Fields.FirstOrDefault(x => x.Name == "category");

            Assert.True(categoryField.Extras.Extra.Count == 2);
            //Apply it
            var db = MongoHelper.GetCollection(_dbConfig, newIntegration.Collection);
            var result = await ht.ApplyToField(categoryField, db);

            Assert.Equal(0, result.ModifiedCount);
            Assert.Equal(0, result.MatchedCount);
            Assert.Equal(2, result.ProcessedRequests.Count);
            Assert.Equal(0, result.Upserts.Count);
            var addResult = await _integrationService.AppendToIntegration(newIntegration, sourceFile, _appAuth);
            Assert.Equal(5402, addResult.Data.ProcessedEntries);
            //Cleanup
            var dbc = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            var dstCollection = new MongoList(dbc.Name, newIntegration.Collection, dbc.GetUrl());
            var category1 = dstCollection.Records.FindSync(Builders<BsonDocument>.Filter.Eq("category", 100000001)).ToList();
            Assert.Equal(6, category1.Count);
            Assert.Equal(10804, dstCollection.Size);
            dstCollection.Trash();
        }
    }
}
