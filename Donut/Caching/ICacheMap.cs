using System.Collections.Generic;
using StackExchange.Redis;

namespace Donut.Caching
{
    public interface ICacheMap
    {
        List<HashEntry> ToHash(object o);
        void Merge(ref HashEntry oldCache, HashEntry newCache);
    }
    public interface ICacheMap<T> : ICacheMap
        where T : class
    {
        void Map();
        T Merge(T oldCache, T newCache);
        string GetKey(params string[] segments);
        RedisValue SerializeValue(T val);
        T DeserializeHash(HashEntry[] hashMembers);
    }
}