using System;
using System.Dynamic;
using System.IO;
using Netlyt.Service;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.IntegrationSource;
using Netlyt.Service.Source;
using Netlyt.ServiceTests.IntegrationSource;
using Netlyt.ServiceTests.Properties;
using Xunit;

namespace Netlyt.ServiceTests
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
            var fs = FileSource.Create(resStream, new JsonFormatter<ExpandoObject>()); 
            var type = fs.ResolveIntegrationDefinition();
            Assert.NotNull(type);
            Assert.True(type.Fields.Count == 2); 
        }
    }
}
