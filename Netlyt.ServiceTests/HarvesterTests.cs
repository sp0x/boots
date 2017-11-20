using System;
using System.Diagnostics;
using System.IO;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.IntegrationSource;
using Xunit;

namespace Netlyt.ServiceTests
{
    [Collection("Entity Parsers")]
    public class HarvesterTests
    {
        /// <summary>
        /// Test destination processing completion, synchronization returnins.
        /// </summary>
        /// <param name="inputDirectory"></param>
        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void TestPipelineCompletion(string inputDirectory)
        {
            var threadCount = 8;
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());
            var userId = "123123123";
            var harvester = new Service.Harvester<IntegratedDocument>(threadCount);
            harvester.LimitShards(1);
            harvester.LimitEntries(10);
            var outBlock = new IntegrationActionBlock(userId, (action, x) => { });
            harvester.SetDestination(outBlock);
            harvester.AddPersistentType(fileSource, userId, null); 
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
            var threadCount = 8;
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter()); 
            var userId = "123123123";
            var harvester = new Service.Harvester<IntegratedDocument>(threadCount);
            harvester.LimitShards(1);
            harvester.LimitEntries(10);
            var outBlock = new IntegrationActionBlock(userId, (action, x) => { });
            harvester.SetDestination(outBlock);
            harvester.AddPersistentType(fileSource, userId, null);
            Assert.True(harvester.Sets.Count > 0);
            var hresult = await harvester.Synchronize();
            Assert.True(hresult.ProcessedEntries == 10);
            Assert.True(hresult.ProcessedShards == 1);
        }

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void MultipleSourceInput(string inputDirectory)
        {
            var threadCount = 20;
            inputDirectory = Path.Combine(Environment.CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());
            var type = fileSource.GetTypeDefinition() as IntegrationTypeDefinition;
            
            Assert.NotNull(type);
            var userId = "123123123"; 
            var harvester = new Service.Harvester<IntegratedDocument>(threadCount);
            harvester.LimitEntries(3);

            Assert.Equal(harvester.ThreadCount, threadCount);
            type.UserId = userId; 
            var outBlock = new IntegrationActionBlock(userId, (action, x) =>
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