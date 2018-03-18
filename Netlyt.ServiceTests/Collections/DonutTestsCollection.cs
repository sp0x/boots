using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.IntegrationSource
{
    [CollectionDefinition("DonutTests")]
    public class DonutTestsCollection : ICollectionFixture<DonutConfigurationFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}