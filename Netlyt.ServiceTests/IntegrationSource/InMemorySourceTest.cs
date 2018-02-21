using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Format;
using Netlyt.Service.IntegrationSource;
using Netlyt.Service.Source;
using Xunit;

namespace Netlyt.ServiceTests.IntegrationSource
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
            var type = inMemoryTest.ResolveIntegrationDefinition(); 
            Assert.NotNull(type);
        }
    }
}
