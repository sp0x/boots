using System.Collections.Generic;
using Netlyt.Interfaces;

namespace Donut.Data
{
    public interface ICacheSet<T> : ICacheSet
        where T : class
    {
        bool ContainsKey(string key);
        void Add(T uuid);
        void Add(string key, T value);
        T GetOrAddHash(string key);
        IEnumerable<T> GetSet();
        IDictionary<string, T> GetHashes();
    }

    public interface ICacheSet
    {
        CacheType Type { get; }
        void SetType(CacheType backingType);
        string Name { get; set; }
        void Cache();
        void ClearLocalCache();
        long Count();
        void Truncate();
    }
}