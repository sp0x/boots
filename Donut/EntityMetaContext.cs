using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Donut.Caching;
using Netlyt.Interfaces;

namespace Donut
{
    /// <summary>
    /// 
    /// </summary>
    public class EntityMetaContext
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        /// <summary>
        /// Category -> value, score. Stored in hashes.
        /// </summary>
        private ConcurrentDictionary<int, Dictionary<string, Score>> _metaValues;
        /// <summary>
        /// Category -> value, flag
        /// </summary>
        private ConcurrentDictionary<uint, Dictionary<string, SetFlags>> _setFlags;
        /// <summary>
        /// A dict of metaCategory , ( metaValue, values ). Stored in sets.
        /// </summary>
        private ConcurrentDictionary<uint, ConcurrentDictionary<string, HashSet<string>>> _entityMetaValues;
        //private RedisCacher _cacher;

        public EntityMetaContext()
        {
            _metaValues = new ConcurrentDictionary<int, Dictionary<string, Score>>();
            _entityMetaValues = new ConcurrentDictionary<uint, ConcurrentDictionary<string, HashSet<string>>>();
            _setFlags = new ConcurrentDictionary<uint, Dictionary<string, SetFlags>>();
            //_entityMetaValues = new ConcurrentDictionary<string, Dictionary<int, HashSet<string>>>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<int, Dictionary<string, Score>> GetMetaValuesWithScores()
        {
            return _metaValues;
        }

        public ConcurrentDictionary<uint, ConcurrentDictionary<string, HashSet<string>>> GetEntityMetaValues()
        {
            return _entityMetaValues;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool SetIsSorted(uint category, string key)
        {
            if (!_setFlags.ContainsKey(category)) return false;
            if (!_setFlags[category].ContainsKey(key)) return false;
            var flags = _setFlags[category][key];
            return flags.IsSorted;
        }


        /// <summary>
        /// Increments the score for a field in a category.
        /// </summary>
        /// <param name="metaCategory">Meta category id</param> 
        /// <param name="metaValue">The value to increment</param>
        public void IncrementMetaCategory(int metaCategory, string metaValue)
        {
            _lock.EnterWriteLock();
            if (!_metaValues.ContainsKey(metaCategory))
            {
                _metaValues[metaCategory] = new Dictionary<string, Score>();
            }

            if (!_metaValues[metaCategory].ContainsKey(metaValue))
            {
                _metaValues[metaCategory][metaValue] = new Score();
            }
            _metaValues[metaCategory][metaValue] += 1;

            if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
        }

        public void AddEntityMetaCategory(string entitykey, uint metaCategory, double metaValue, bool sorted = false)
        {
            AddEntityMetaCategory(entitykey, metaCategory, metaValue.ToString(), sorted);
        }
        /// <summary>
        /// Adds a meta value of a given category and entity to a cache set.
        /// </summary>
        /// <param name="entitykey"></param>
        /// <param name="metaCategory"></param>
        /// <param name="metaValue"></param>
        public void AddEntityMetaCategory(string entitykey, uint metaCategory, string metaValue, bool sorted = false)
        {
            _lock.EnterWriteLock();
            if (!_entityMetaValues.ContainsKey(metaCategory))
            {
                _entityMetaValues[metaCategory] = new ConcurrentDictionary<string, HashSet<string>>();
            }
            if (!_entityMetaValues[metaCategory].ContainsKey(entitykey))
            {
                _entityMetaValues[metaCategory][entitykey] = new HashSet<string>();
            }
            _entityMetaValues[metaCategory][entitykey].Add(metaValue);
            if (sorted)
            {
                if (!_setFlags.ContainsKey(metaCategory))
                {
                    _setFlags[metaCategory] = new Dictionary<string, SetFlags>();
                }
                if (!_setFlags[metaCategory].ContainsKey(entitykey))
                {
                    _setFlags[metaCategory][entitykey] = new SetFlags(false);
                }
                _setFlags[metaCategory][entitykey] = _setFlags[metaCategory][entitykey] = new SetFlags(true);
            }
            if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
        }

        public HashSet<String> GetEntityMetaValues(string key, uint category)
        {
            _lock.EnterWriteLock();
            if (!_entityMetaValues.ContainsKey(category))
            {
                _entityMetaValues[category] = new ConcurrentDictionary<string, HashSet<string>>();
            }
            if (!_entityMetaValues[category].ContainsKey(key))
            {
                _entityMetaValues[category][key] = new HashSet<string>();
            }
            HashSet<string> collection = _entityMetaValues[category][key];
            return collection;
        }
        /// <summary>
        /// 
        /// </summary>
        protected void ClearMetaValues()
        {
            _metaValues.Clear();
        }
        /// <summary>
        /// 
        /// </summary>
        protected void ClearEntityMetaValues()
        {
            _entityMetaValues.Clear();
        }


        /// <summary>
        /// Gets the key to a meta value
        /// </summary>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected string GetValueKey(int category, string key)
        {
            var fqkey = $"_mv:{category}";
            if (!string.IsNullOrEmpty(key)) fqkey += $":{key}";
            return fqkey;
        }
    }
}