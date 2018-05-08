using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Donut.Data;
using Netlyt.Interfaces;

namespace Donut.Caching
{

    public abstract class CacheSet<T> : ICacheSet<T>
        where T : class
    {
        private ConstructorInfo _constructor;

        public CacheType Type { get; protected set; }
        public string Name { get; set; }
        public abstract void SetType(CacheType backingType);
        public abstract void Cache();
        public abstract Task CacheAsync();

        public virtual void ClearLocalCache()
        { 
        }

        public CacheSet()
        {
            Type = CacheType.Set;
            _constructor = typeof(T).GetConstructor(new Type[] { });
        }


        public abstract bool ContainsKey(string key);
        public abstract void Add(T uuid);
        public abstract void Add(string key, T value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract T GetOrAddHash(string key);
        /// <summary>
        /// Gets all elements in a set
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<T> GetSet();
        /// <summary>
        /// Gets all hashes in the cache.
        /// </summary>
        /// <returns></returns>
        public abstract IDictionary<string, T> GetHashes();
        public abstract T AddOrMerge(string pageSelector, T pageStats);

        public abstract long Count();
        public abstract void Truncate();
    }
}