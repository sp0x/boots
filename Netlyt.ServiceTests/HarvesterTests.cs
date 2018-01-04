using System;
using System.Diagnostics;
using System.IO;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.IntegrationSource;
using Netlyt.ServiceTests.IntegrationSource;
using Xunit;

namespace Netlyt.ServiceTests
{
    [Collection("Entity Parsers")]
    public class HarvesterTests
    {

        private DynamicContextFactory _contextFactory;
        private ApiService _apiService;
        private ConfigurationFixture _config;
        private IntegrationService _integrationService;

        public HarvesterTests(ConfigurationFixture fixture)
        {
            _config = fixture;
            _contextFactory = new DynamicContextFactory(() => _config.CreateContext());
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
        }

        /// <summary>
        /// Test destination processing completion, synchronization returnins.
        /// </summary>
        /// <param name="inputDirectory"></param>
        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void TestPipelineCompletion(string inputDirectory)
        {
            var threadCount = (uint)8;
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());
            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Service.Harvester<IntegratedDocument>(_apiService, _integrationService, threadCount);
            harvester.LimitShards(1);
            harvester.LimitEntries(10);
            var outBlock = new IntegrationActionBlock(appId, (action, x) => { });
            harvester.SetDestination(outBlock);
            harvester.AddPersistentType(fileSource, apiObj, null); 
            var hresult = await harvester.Synchronize();
            Assert.True(outBlock.ProcessingCompletion.IsCompleted);
            Assert.True(outBlock.BufferCompletion.IsCompleted);
        }

        /// <summary>
        /// Test input limiting
        /// </summary>
        /// <param name="inputDirectory"></param>
        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void LimitedEntries(string inputDirectory)
        {
            var threadCount = (uint)8;
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter()); 
            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Service.Harvester<IntegratedDocument>(_apiService, _integrationService, threadCount);
            harvester.LimitShards(1);
            harvester.LimitEntries(10);
            var outBlock = new IntegrationActionBlock(appId, (action, x) => { });
            harvester.SetDestination(outBlock);
            harvester.AddPersistentType(fileSource, apiObj, null);
            Assert.True(harvester.Sets.Count > 0);
            var hresult = await harvester.Synchronize();
            Assert.True(hresult.ProcessedEntries == 10);
            Assert.True(hresult.ProcessedShards == 1);
        }

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void MultipleSourceInput(string inputDirectory)
        {
            var threadCount = (uint)20;
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());
            var type = fileSource.GetTypeDefinition() as DataIntegration;
            
            Assert.NotNull(type);
            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Service.Harvester<IntegratedDocument>(_apiService, _integrationService, threadCount);
            harvester.LimitEntries(3);

            Assert.Equal(harvester.ThreadCount, threadCount);
            type.APIKey = apiObj; 
            var outBlock = new IntegrationActionBlock(appId, (action, x) =>
            { });
            harvester.SetDestination(outBlock);
            harvester.AddType(type, fileSource);
            Assert.True(harvester.Sets.Count > 0);
            await harvester.Synchronize();
            Assert.True(harvester.ElapsedTime().TotalMilliseconds > 0);
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}"); 
        }
    }
}