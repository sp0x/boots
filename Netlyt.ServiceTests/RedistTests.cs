using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using nvoid.db.Caching;
using Xunit;

namespace Netlyt.ServiceTests
{
    [Collection("Entity Parsers")]
    public class RedisTests
    {
        private RedisCacher _cache;

        public RedisTests(ConfigurationFixture fixture)
        {
            _cache = fixture.GetService<RedisCacher>();
        } 

        [Fact]
        public void TestIncrement()
        {
            _cache.Set("a", 1);
            _cache.Increment("a","data");
            var value = _cache.GetInt("a");
            Assert.Equal(2, value);
        }
    }

}
