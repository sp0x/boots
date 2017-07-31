using System;
using System.IO;
using Peeralize.Service;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;
using Peeralize.ServiceTests.IntegrationSource;
using Peeralize.ServiceTests.Properties;
using Xunit;

namespace Peeralize.ServiceTests
{
//    public static class Mainx
//    {
//        static void Main()
//        {
//            IntegrationTest test = new IntegrationTest();
//            test.TestFileInput("testFileInputData.json");
//        }
//    }

    [Collection("Data Sources")]
    public class IntegrationTest
    {
        
        private ConfigurationFixture _config;

        public IntegrationTest(ConfigurationFixture fixture)
        {
            _config = fixture;
        }

        [Theory]
        [InlineData("testHarvesterInput")]
        public void TestFileInput(String file)
        {
            var resBytes = Resources.ResourceManager.GetObject(file);
            var resStream = new MemoryStream(resBytes as byte[]);
            var fs = FileSource.Create(resStream, new JsonFormatter()); 
            var type = fs.GetTypeDefinition();
            Assert.NotNull(type);
            Assert.True(type.Fields.Count == 2); 
        }
    }
}
