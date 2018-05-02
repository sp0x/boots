using System;
using System.Dynamic;
using System.IO;
using Donut.IntegrationSource;
using Netlyt.Interfaces.Data.Format;
using Netlyt.ServiceTests.Fixtures;
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
        
        private ConfigurationFixture _fixture;

        public IntegrationTest(ConfigurationFixture fixture)
        {
            _fixture = fixture;
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
