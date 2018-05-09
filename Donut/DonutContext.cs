using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Donut.Caching;
using Donut.Data;
using Donut.Integration;
using MongoDB.Driver;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using StackExchange.Redis;

namespace Donut
{
 
    /// <summary>
    /// Contains all data references for a donut
    /// </summary>
    public class DonutContext : EntityMetaContext, ISetCollection, IDisposable, IDonutContext
    {
        private readonly object _cacheLock = new object();
        private readonly IRedisCacher _cacher;
        public IIntegration Integration { get; set; }
        private ConcurrentDictionary<string, List<HashEntry>> CurrentCache { get; set; }
        private readonly IDictionary<Type, ICacheSet> _sets = new Dictionary<Type, ICacheSet>();
        private readonly IDictionary<Type, IDataSet> _dataSets = new Dictionary<Type, IDataSet>(); 

        public string Prefix { get; set; }
        public IApiAuth ApiAuth { get; private set; }
        public IRedisCacher Database => _cacher;
        private int _currentCacheRunIndex;
        private CachingPersistеnceService _cachingService;

        /// <summary>
        /// The entity interval on which to cache the values.
        /// </summary>
        public int CacheRunInterval { get; private set; }
        public DonutContext(IRedisCacher cacher, Data.DataIntegration integration, IServiceProvider serviceProvider)
        {
            _cacher = cacher;
            ApiAuth = integration.APIKey;
            CacheRunInterval = 10;
            _currentCacheRunIndex = 0;
            Integration = integration;
            CurrentCache = new ConcurrentDictionary<string, List<HashEntry>>();
            ConfigureCacheMap();
            Prefix = $"integration_context:{Integration.Id}";
            new ContextSetDiscoveryService(this, serviceProvider).Initialize();
            _cachingService = new CachingPersistеnceService(this);
            if (integration != null && !string.IsNullOrEmpty(integration.FeaturesCollection))
            {
//                var dbConfig = DBConfig.GetGeneralDatabase();
//                var mongoList = new MongoList(dbConfig, integration.FeaturesCollection);//Make sure the collection exists
//                var murlBuilder = new MongoUrlBuilder("");
//                murlBuilder.AuthenticationSource = "admin";
//                var murl = murlBuilder.ToMongoUrl();
//                var connection = new MongoClient(murl);
//                var database = connection.GetDatabase("");
                IMongoDatabase db = MongoHelper.GetDatabase();
                db.CreateCollection(integration.FeaturesCollection);
            }
        }

        /// <summary>
        /// Saves a group
        /// </summary>
        /// <param name="aggKeyBuff"></param>
        /// <returns></returns>
        public uint AddMetaGroup(Dictionary<string, object> aggKeyBuff)
        {
            var index = _cachingService.AddHashWithIndex(Prefix, aggKeyBuff);
            return index;
        }

        public void SetCacheRunInterval(int interval)
        {
            if (interval < 0 || interval == 0) return;
            CacheRunInterval = interval;
        }
        ICacheSet ISetCollection.GetOrAddSet(ICacheSetSource source, Type type)
        {
            if (!_sets.TryGetValue(type, out var set))
            {
                set = source.Create(this, type);
                _sets[type] = set;
            }
            return set;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public IDataSet GetOrAddDataSet(ICacheSetSource source, Type type)
        {
            if (!_dataSets.TryGetValue(type, out var set))
            {
                set = source.CreateDataSet(this, type);
                _dataSets[type] = set;
            }
            return set;
        }

        public double MetaEntityMax(string key, int category, double @default)
        {
            var fqkey = GetMetaCacheKey(category, key);
            var entityMetaMaxValue = _cacher.GetSortedSetMax(fqkey);
            return entityMetaMaxValue!=null ? entityMetaMaxValue.Value.Score : @default;
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
                CacheMetaContext();
                ClearMetaContext();
            }

        }

        public void Complete()
        {
            CacheAndClear(true);
        }

        private void ClearMetaContext()
        {
            ClearMetaValues();
            ClearEntityMetaValues();
        }

        private string GetMetaCacheKey(int category, string key)
        {
            var baseKey = base.GetValueKey(category, key);
            var categoryKey = $"{Prefix}:{baseKey}";
            return categoryKey;
        }

        /// <summary>
        /// 
        /// </summary>
        public void TruncateSets()
        {
            lock (_cacheLock)
            { 
                foreach (var set in _sets.Values)
                {
                    set.Truncate();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metaSpentTime"></param>
        public void TruncateMeta(int metaSpentTime)
        {
            var subKey = base.GetValueKey(metaSpentTime, "");
            var key = $"{Prefix}:{subKey}";
            _cachingService.Truncate(key);
        }
        /// <summary>
        /// Gets the set of a size.
        /// </summary>
        /// <param name="setKey"></param>
        /// <returns></returns>
        public long GetSetSize(string setKey)
        {
            return _cachingService.GetSetSize(setKey);
        }

        /// <summary>
        /// Caches all the meta categories and values.
        /// </summary>
        private void CacheMetaContext()
        {
            //metaCategory->(meta value->score)
            var metaCategoryScores = base.GetMetaValuesWithScores();
            foreach (var categoryPair in metaCategoryScores)
            {
                var categoryId = categoryPair.Key;
                var categoryKey = $"{Prefix}:_m:{categoryId}";
                foreach (var val in categoryPair.Value)
                {
                    var fullKey = $"{categoryKey}:{val.Key}";
                    var cntHash = new HashEntry("count", val.Value.Count);
                    var scoreHash = new HashEntry("score", val.Value.Value);
                    _cacher.SetHash(fullKey, cntHash);
                    _cacher.SetHash(fullKey, scoreHash); 
                }
            }
            //metaCategory->(metaValue->element set)
            var metaCategoryValueSets = base.GetEntityMetaValues();
            foreach (var categoryPair in metaCategoryValueSets)
            {
                uint categoryId = categoryPair.Key;
                var categoryKey = $"{Prefix}:_mv:{categoryId}";
                foreach (var metaVal in categoryPair.Value)
                {
                    var isSorted = SetIsSorted(categoryId, metaVal.Key);
                    var fullKey = $"{categoryKey}:{metaVal.Key}";
                    var set = metaVal.Value;
                    if (isSorted)
                    {
                        //Generate scores from x
                        _cacher.SortedSetAddAll(fullKey, set.Select(x =>
                        {
                            double score = 0;
                            try
                            {
                                score = (double) double.Parse(x);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine($"Malformed data (double): {ex.Message}");
                            }
                            var entry = new SortedSetEntry((RedisValue) x, score);
                            return entry;
                        }));
                    }
                    else
                    {
                        _cacher.SetAddAll(fullKey, set.Select(x => (RedisValue)x));
                    }
                }
            }
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