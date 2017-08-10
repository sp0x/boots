﻿using Xunit;

namespace Peeralize.ServiceTests.IntegrationSource
{
    [CollectionDefinition("Entity Parsers")]
    public class EntityParsingCollection : ICollectionFixture<ConfigurationFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}