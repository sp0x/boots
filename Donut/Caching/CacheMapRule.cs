using System;
using System.Linq.Expressions;
using nvoid.db.Caching;
using StackExchange.Redis;

namespace Donut.Caching
{
    public class CacheMapRule<T>
        where T : class
    {
        private CacheMap<T> _cacheMap;
        private CacheMember cMember;
        private Expression<Func<T, RedisValue>> fetcher;
        private Func<RedisValue, object> _deserializer;


        public CacheMapRule(CacheMap<T> cacheMap, CacheMember cMember, Expression<Func<T, RedisValue>> fetcher)
        {
            this._cacheMap = cacheMap;
            this.cMember = cMember;
            this.fetcher = fetcher;
        }

        public CacheMap<T> Merge(Action<T, T> merger)
        {
            _cacheMap.MergeBy(cMember, merger);
            return _cacheMap;
        }

        public CacheMapRule<T> AddMember(Expression<Func<T, RedisValue>> f, string key = null)
        {
            return _cacheMap.AddMember(f, key);
        } 

        public CacheMapRule<T> DeserializeAs<TValue>(Func<RedisValue, TValue> func)
        {
            _deserializer = (x)=> func(x);
            _cacheMap.AddDeserializer(cMember, _deserializer);
            return this;
        }
    }
}