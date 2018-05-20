using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Donut;
using Donut.Data;
using Donut.Data.Format;
using Donut.Encoding;
using Donut.IntegrationSource;
using Donut.Orion;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.extensions;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.ServiceTests.Fixtures;
using StackExchange.Redis;
using Xunit;

namespace Netlyt.ServiceTests.IntegrationTests
{
    [Collection("DonutTests")]
    public class IntegrationClenaupTests
    {
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

        public IntegrationClenaupTests(DonutConfigurationFixture fixture)
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

        //Churn_Modelling.csv
        [Theory]
        [InlineData(new object[] { "TestData\\Churn_Modelling.csv" })]
        public async Task ChurnModelling(string inputFile)
        {
            inputFile = Path.Combine(Environment.CurrentDirectory, inputFile);
            var formatter = new CsvFormatter<ExpandoObject>() { Delimiter = ',' };
            var fileSource = FileSource.CreateFromFile(inputFile, formatter);
            //formatter.FieldsToIgnore.Add("uuid");
            fileSource.Field("RowNumber").Ignore();
            fileSource.Field("CustomerId").Ignore();
            fileSource.Field("CustomerId").Ignore();
            fileSource.Field("Gender").SetValue(x => x["Gender"].ToString().ToLower() == "female" ? 0 : 1);
            //_integrationService.ResolveFormatter<ExpandoObject>("text/csv");

            fileSource.SetFormatter(formatter);
            var ignName = "ChurnModelling";
            DataImportTask<ExpandoObject> importTask = _integrationService.CreateIntegrationImportTask(fileSource, _appAuth, _user, ignName);
            var ign = DataIntegration.Wrap(importTask.Integration);
            _db.Integrations.Add(ign);
            _db.SaveChanges();
            importTask.Options.ShardLimit = 1;
            //importTask.Options.TotalEntryLimit = 100;
            var importResult = await importTask.Import();
            AggregateOptions options = new AggregateOptions() { AllowDiskUse = true, BatchSize = 1000 };
             
            var csDoc = new CsvWriter($"{ignName}.csv");
            csDoc.WriteLine("id", "ondate", "event", "type", "value", "paid");
            var col = importResult.Collection;
            MongoDB.Driver.PipelineDefinition<BsonDocument, BsonDocument> pipeline = new BsonDocument[] {
                new BsonDocument("$sort", new BsonDocument("ondate", 1)) ,
            };
            var sortedEvents = col.Aggregate(pipeline, options)
                .ToEnumerable();
            foreach (var ev in sortedEvents)
            {  
                List<string> values = new List<string>();//uuid, ondate, event_id, type, real_value
                values.Add(ev["uuid"].ToString());
                values.Add(ev["ondate"].ToString());
                values.Add(ev["event_id"].ToString());
                values.Add(ev["type"].ToString());
                values.Add(ev["real_value"].ToString());
                csDoc.WriteLine(values.ToArray());
            }
            col.Drop();
        }
    }
}
