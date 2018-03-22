using System; 
using System.Collections.Concurrent;
using System.Collections.Generic; 
using System.Linq; 
using nvoid.db;
using nvoid.db.Caching; 
using nvoid.Integration;
using Netlyt.Service.Integration; 
using StackExchange.Redis;

namespace Netlyt.Service.Donut
{
 
    /// <summary>
    /// 
    /// </summary>
    public class DonutContext : EntityMetaContext, ISetCollection, IDisposable
    {
        private readonly object _cacheLock = new object();
        private readonly RedisCacher _cacher;
        public DataIntegration Integration { get; set; }
        private ConcurrentDictionary<string, List<HashEntry>> CurrentCache { get; set; }
        private readonly IDictionary<Type, ICacheSet> _sets = new Dictionary<Type, ICacheSet>();
        private readonly IDictionary<Type, IDataSet> _dataSets = new Dictionary<Type, IDataSet>(); 

        public string Prefix { get; set; }
        public ApiAuth ApiAuth { get; private set; }
        public RedisCacher Database => _cacher;
        private int _currentCacheRunIndex;
        private CachingPersistеnceService _cachingService;

        /// <summary>
        /// The entity interval on which to cache the values.
        /// </summary>
        public int CacheRunInterval { get; private set; }
        public DonutContext(RedisCacher cacher, DataIntegration integration, IServiceProvider serviceProvider)
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
        /// Caches all the meta categories and values
        /// </summary>
        private void CacheMetaContext()
        {
            //metaCategory->(meta value->score)
            var metaCategoryScores = base.GetMetaValues();
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
                int categoryId = categoryPair.Key;
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
                            var score = (double)double.Parse(x);
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