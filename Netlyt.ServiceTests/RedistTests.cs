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
            var watch = new Stopwatch();
            var dict = new Dictionary<string, int>();
            dict["a"] = 0;
            dict["b"] = 0;
            dict["c"] = 0;
            watch.Start();
            //Op1
            for (var i = 0; i < 10000; i++)
            {
                dict["c"]++;
            }
            _cache.SetHash("a", dict);
            watch.Stop();
            var t1 = watch.Elapsed.TotalSeconds;

            dict["a"] = 0;
            dict["b"] = 0;
            dict["c"] = 0;
            watch.Restart();
            //Op2
            _cache.Set("a", 0);
            for (var i = 0; i < 10000; i++)
            {
                _cache.Increment("a", "data");
            }
            watch.Stop();
            var t2 = watch.Elapsed.TotalSeconds;
            Console.WriteLine(t1);
            Console.WriteLine(t2);

            _cache.Set("a", 1);
            _cache.Increment("a","data");
            var value = _cache.GetInt("a");
            Assert.Equal(2, value);
        }
    }

}
