using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using nvoid.db.DB;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.IntegrationSource;
using Netlyt.ServiceTests.IntegrationSource;
using Xunit;

namespace Netlyt.ServiceTests.Integration.Blocks
{
    [Collection("Entity Parsers")]
    public class GroupingBlockTests
    {

        private DynamicContextFactory _contextFactory;
        private ApiService _apiService;
        private ConfigurationFixture _config;
        private IntegrationService _integrationService;

        public GroupingBlockTests(ConfigurationFixture fixture)
        {
            _config = fixture;
            _contextFactory = new DynamicContextFactory(() => _config.CreateContext());
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
        }

        private GroupingBlock GetGrouper(string userId)
        {
            var grouper = new GroupingBlock(userId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.Day}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                (accumulated, document) =>
                {
                    accumulated.AddDocumentArrayItem("events", new
                    {
                        ondate = document["ondate"].ToString(),
                        event_id = document.GetInt("event_id"),
                        type = document.GetInt("type"),
                        value = document["value"]?.ToString()
                    }.ToBsonDocument());
                    return null;
                });
            return grouper;
        }


        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void TestSimpleGroup(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment
                .CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter()); 
            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Harvester<IntegratedDocument>(_apiService, _integrationService, 20);
            var grouper = GetGrouper(appId);
            harvester.LimitEntries(10);
            harvester.SetDestination(grouper); 
            harvester.AddPersistentType(fileSource, apiObj, null);
            var results = await harvester.Synchronize();
            Assert.True(results.ProcessedEntries == 10 && grouper.EntityDictionary.Count > 0);
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
        }

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void TestTransformationGroup(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment
                .CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());
            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Netlyt.Service.Harvester<IntegratedDocument>(_apiService, _integrationService, 20);

            var grouper = GetGrouper(appId);
            var statsCounter = 0;
            var statsBlock = new StatsBlock((visit) =>
            { 
                Interlocked.Increment(ref statsCounter);
                Thread.Sleep(1000);
            }); 
            grouper.LinkTo(statsBlock, null);
            harvester.LimitEntries(10);
            harvester.SetDestination(grouper);
            harvester.AddPersistentType(fileSource, apiObj, null);
            var results = await harvester.Synchronize();
            //Assert.True(results.ProcessedEntries == 10 && grouper.EntityDictionary.Count > 0);
            //Ensure that we went through all the items, with our entire dataflow.
            Assert.True(results.ProcessedEntries == statsCounter);
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
        }
    }
}
