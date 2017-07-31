using System.Collections.Generic;
using System.Text;
using Peeralize.Service.Format;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;
using Xunit;

namespace Peeralize.ServiceTests.IntegrationSource
{
    [Collection("Data Sources")]
    public class InMemorySourceTest 
    {
        private ConfigurationFixture _config;

        public InMemorySourceTest(ConfigurationFixture fixture)
        {
            _config = fixture;
        }

        [Theory]
        [InlineData("{ name : \"Pesho\", age : 3}")]
        public void GetNextTest(string input)
        {
            var inMemoryTest = new InMemorySource(input, new JsonFormatter());
            var type = inMemoryTest.GetTypeDefinition(); 
            Assert.NotNull(type);
        }
    }
}
