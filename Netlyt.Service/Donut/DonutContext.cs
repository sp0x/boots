using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using nvoid.db.Caching;
using nvoid.db.Extensions;
using Netlyt.Service.Integration;
using Netlyt.Service.Models;
using Netlyt.Service.Models.CacheMaps;
using StackExchange.Redis;

namespace Netlyt.Service.Donut
{
    /// <summary>
    /// 
    /// </summary>
    public class DonutContext : EntityMetaContext, ICacheSetCollection, IDisposable
    {
        private readonly object _cacheLock = new object();
        private readonly RedisCacher _cacher;
        public DataIntegration Integration { get; set; }
        private ConcurrentDictionary<string, List<HashEntry>> CurrentCache { get; set; }
        private readonly IDictionary<Type, ICacheSet> _sets = new Dictionary<Type, ICacheSet>();
        public string Prefix { get; set; }
        public RedisCacher Database => _cacher;
        private int _currentCacheRunIndex;

        /// <summary>
        /// The entity interval on which to cache the values.
        /// </summary>
        public int CacheRunInterval { get; private set; }
        public DonutContext(RedisCacher cacher, DataIntegration integration)
        {
            _cacher = cacher;
            CacheRunInterval = 10000;
            _currentCacheRunIndex = 0;
            Integration = integration;
            CurrentCache = new ConcurrentDictionary<string, List<HashEntry>>();
            ConfigureCacheMap();
            Prefix = $"integration_context:{Integration.Id}";
            new ContextSetDiscoveryService(this).Initialize();
        }

        public void SetCacheRunInterval(int interval)
        {
            if (interval < 0 || interval == 0) return;
            CacheRunInterval = interval;
        }
        ICacheSet ICacheSetCollection.GetOrAddSet(ICacheSetSource source, Type type)
        {
            if (!_sets.TryGetValue(type, out var set))
            {
                set = source.Create(this, type);
                _sets[type] = set;
            }
            return set;
        }

        /// <summary>
        /// Caches all the properties
        /// </summary>
        public void Cache(bool force = false)
        {
            lock (_cacheLock)
            {
                if (!force && _currentCacheRunIndex < CacheRunInterval)
                {
                    _currentCacheRunIndex++;
                    return;
                }
                _currentCacheRunIndex = 0;
            }
            //Go over each cache set, and update.
            foreach (var set in _sets.Values)
            {
                set.Cache();
            }
            CacheMetaContext();
        }

        /// <summary>
        /// Caches all the properties
        /// </summary>
        public void CacheAndClear(bool force = false)
        {
            lock (_cacheLock)
            {
                if (!force && _currentCacheRunIndex < CacheRunInterval)
                {
                    _currentCacheRunIndex++;
                    return;
                }
                _currentCacheRunIndex = 0;

                //Go over each cache set, and update.
                foreach (var set in _sets.Values)
                {
                    set.Cache();
                    //Clear the set
                    set.ClearLocalCache();
                }
                //CacheMetaContext();
            }

        }

        private void CacheMetaContext()
        {
            var meta = base.GetMetaValues();
            var entityMeta = base.GetEntityMetaValues();
        }


        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cache(true);
                _cacher?.Dispose();
            }
        }

        protected virtual void ConfigureCacheMap()
        {
            //This is just a stub..
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}