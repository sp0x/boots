using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; 
using nvoid.db.Caching;
using nvoid.db.Extensions;
using Netlyt.Service.Integration;
using Netlyt.Service.Models;
using Netlyt.Service.Models.CacheMaps;
using StackExchange.Redis;

namespace Netlyt.Service.Donut
{
    public interface ICacheSetFinder
    { 
        IReadOnlyList<CacheSetProperty> FindSets(DonutContext context);
    }

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
        private CachingPersistanceService CachingService { get; set; }

        /// <summary>
        /// The entity interval on which to cache the values.
        /// </summary>
        private int CacheInterval { get; set; }
        public DonutContext(RedisCacher cacher, DataIntegration integration)
        {
            _cacher = cacher;
            CacheInterval = 100;
            Integration = integration;
            CurrentCache = new ConcurrentDictionary<string, List<HashEntry>>();
            ConfigureCacheMap();
            Prefix = $"integration_context:{Integration.Id}";
            new ContextSetDiscoveryService(this).Initialize();
            CachingService = new CachingPersistanceService(this); 
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
        public void Cache()
        {
            lock (_cacheLock)
            {
                //Go over each cache set, and update.
                foreach (var set in _sets)
                {
                    set.Value.Cache(CachingService);
                }
                CacheMetaContext();
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