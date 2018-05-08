using System;
using System.Collections.Generic;
using System.Diagnostics;
using Donut.Caching;
using nvoid.db.Caching;
using Netlyt.ServiceTests.Fixtures;
using StackExchange.Redis;
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
        public void HashTest()
        {
            var watch = new Stopwatch();
            watch.Start();
            var a = 1;
            for (int i = 0; i < 1000000; i++)
            {
                var b = 1;
                a += b;
            }
            var t1 = watch.Elapsed.TotalSeconds;
            watch.Stop();
            watch.Reset();
            watch.Start();
            var hv = new HashEntry("a", 1);
            for (int i = 0; i < 1000000; i++)
            {
                HashEntry b = new HashEntry("a", 1);
                hv = new HashEntry(hv.Name, hv.Value + b.Value);
            }
            var t2 = watch.Elapsed.TotalSeconds;
            watch.Stop();
            watch.Reset();
            Console.WriteLine(t1);
            Console.WriteLine(t2);
            watch.Stop();
        }

        [Fact]
        public void TestSetCount()
        {
            var kv = "__key1";
            for (var i=0; i<10; i++) _cache.SetAdd(kv,i);
            var kcount = _cache.GetSetItemCount(kv);
            _cache.Remove("__key1");
            Assert.Equal(10, kcount);
        }
        [Fact]
        public void TestZScanLast()
        {
            var kv = "__key2";
            for (var i = 0; i < 10; i++) _cache.SortedSetAdd(kv, $"lol{i}", i);
            var maxValue = _cache.GetSortedSetMax(kv).Value.Score;
            _cache.Remove(kv);
            Assert.Equal(9, maxValue);
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
