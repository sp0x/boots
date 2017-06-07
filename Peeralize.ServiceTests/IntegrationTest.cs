using System;
using Peeralize.Service;
using Peeralize.Service.Integration;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;
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

    public class IntegrationTest
    {
        [Theory]
        [InlineData("testFileInputData.json")]
        public void TestFileInput(String file)
        {
            var resContent = Resources.ResourceManager.GetString(file);
            var fs = FileSource.Create(file, new JsonFormatter());
            var type = fs.GetTypeDefinition();
            var harvester = new Harvester();
            harvester.SetDestination(new MongoSink(Guid.NewGuid().ToString()));
            harvester.AddType(type, fs);
            harvester.Synchronize();
        }
    }
}
