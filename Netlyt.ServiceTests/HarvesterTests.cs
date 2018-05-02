using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using Donut;
using Donut.Blocks;
using Donut.IntegrationSource;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data.Format;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests
{
    [Collection("Entity Parsers")]
    public class HarvesterTests
    {

        private DynamicContextFactory _contextFactory;
        private ApiService _apiService;
        private ConfigurationFixture _config;
        private IIntegrationService _integrationService;

        public HarvesterTests(ConfigurationFixture fixture)
        {
            _config = fixture;
            _contextFactory = new DynamicContextFactory(() => _config.CreateContext());
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IIntegrationService>();
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
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>());

            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Harvester<IntegratedDocument>(threadCount);
            harvester.LimitShards(1);
            harvester.LimitEntries(10);
            var outBlock = new IntegrationActionBlock<IntegratedDocument>(appId, (action, x) => { });
            harvester.SetDestination(outBlock);
            harvester.AddIntegrationSource(fileSource, apiObj, null); 
            var hresult = await harvester.Run();
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
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>()); 
            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Harvester<IntegratedDocument>(threadCount);
            harvester.LimitShards(1);
            harvester.LimitEntries(10);
            var outBlock = new IntegrationActionBlock<IntegratedDocument>(appId, (action, x) => { });
            harvester.SetDestination(outBlock);
            harvester.AddIntegrationSource(fileSource, apiObj, null);
            Assert.True(harvester.IntegrationSets.Count > 0);
            var hresult = await harvester.Run();
            Assert.True(hresult.ProcessedEntries == 10);
            Assert.True(hresult.ProcessedShards == 1);
        }

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void MultipleSourceInput(string inputDirectory)
        {
            var threadCount = (uint)20;
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>());
            var type = fileSource.ResolveIntegrationDefinition() as DataIntegration;
            
            Assert.NotNull(type);
            var appId = "123123123";
            var apiObj = _apiService.GetApi(appId);
            var harvester = new Harvester<IntegratedDocument>(threadCount);
            harvester.LimitEntries(3);

            Assert.Equal(harvester.ThreadCount, threadCount);
            type.APIKey = apiObj; 
            var outBlock = new IntegrationActionBlock<IntegratedDocument>(appId, (action, x) =>
            { });
            harvester.SetDestination(outBlock);
            harvester.AddIntegration(type, fileSource);
            Assert.True(harvester.IntegrationSets.Count > 0);
            await harvester.Run();
            Assert.True(harvester.ElapsedTime().TotalMilliseconds > 0);
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}"); 
        }
    }
}