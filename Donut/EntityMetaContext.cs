using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Donut.Caching;
using Netlyt.Interfaces;

namespace Netlyt.Service.Donut
{
    public class EntityMetaContext
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        /// <summary>
        /// /
        /// </summary>
        private ConcurrentDictionary<int, Dictionary<string, Score>> _metaValues;

        private ConcurrentDictionary<int, Dictionary<string, SetFlags>> _setFlags;
        /// <summary>
        /// A dict of metaCategory , ( metaValue, userIds )
        /// </summary>
        private ConcurrentDictionary<int, Dictionary<string, HashSet<string>>> _entityMetaValues; 
        //private RedisCacher _cacher;

        public EntityMetaContext()
        { 
            _metaValues = new ConcurrentDictionary<int, Dictionary<string, Score>>();
            _entityMetaValues = new ConcurrentDictionary<int, Dictionary<string, HashSet<string>>>();
            _setFlags = new ConcurrentDictionary<int, Dictionary<string, SetFlags>>();
            //_entityMetaValues = new ConcurrentDictionary<string, Dictionary<int, HashSet<string>>>();
        }

        public ConcurrentDictionary<int, Dictionary<string, Score>> GetMetaValues()
        {
            return _metaValues;
        }

        public ConcurrentDictionary<int, Dictionary<string, HashSet<string>>> GetEntityMetaValues()
        {
            return _entityMetaValues;
        }

        public bool SetIsSorted(int category, string key)
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

        public void AddEntityMetaCategory(string entitykey, int metaCategory, double metaValue, bool sorted = false)
        {
            AddEntityMetaCategory(entitykey, metaCategory, metaValue.ToString(), sorted);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entitykey"></param>
        /// <param name="metaCategory"></param>
        /// <param name="metaValue"></param>
        public void AddEntityMetaCategory(string entitykey, int metaCategory, string metaValue, bool sorted = false)
        {
            _lock.EnterWriteLock();
            if (!_entityMetaValues.ContainsKey(metaCategory))
            {
                _entityMetaValues[metaCategory] = new Dictionary<string, HashSet<string>>();
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

        public HashSet<String> GetEntityMetaValues(string key, int category)
        {
            _lock.EnterWriteLock();
            if (!_entityMetaValues.ContainsKey(category))
            {
                _entityMetaValues[category] = new Dictionary<string, HashSet<string>>();
            }
            if (!_entityMetaValues[category].ContainsKey(key))
            {
                _entityMetaValues[category][key] = new HashSet<string>();
            }
            HashSet<string> collection = _entityMetaValues[category][key];
            return collection;
        }
         
        protected void ClearMetaValues()
        {
            _metaValues.Clear();
        }

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
            if(!string.IsNullOrEmpty(key)) fqkey += $":{key}";
            return fqkey;
        }
    }
}