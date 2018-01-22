using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using nvoid.db.Caching;
using Netlyt.Service.Models;

namespace Netlyt.Service.Donut
{
    public class EntityMetaContext
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private ConcurrentDictionary<int, Dictionary<string, Score>> _metaValues;
        /// <summary>
        /// A dict of metaCategory , ( metaValue, userIds )
        /// </summary>
        private ConcurrentDictionary<int, Dictionary<string, HashSet<string>>> _entityMetaValues; 
        public EntityMetaContext()
        { 
            _metaValues = new ConcurrentDictionary<int, Dictionary<string, Score>>();
            _entityMetaValues = new ConcurrentDictionary<int, Dictionary<string, HashSet<string>>>();
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
        /// <summary>
        /// 
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entitykey"></param>
        /// <param name="metaCategory"></param>
        /// <param name="metaValue"></param>
        public void AddEntityMetaCategory(string entitykey, int metaCategory, string metaValue)
        {
            _lock.EnterWriteLock();
            if (!_entityMetaValues.ContainsKey(metaCategory))
            {
                _entityMetaValues[metaCategory] = new Dictionary<string, HashSet<string>>();
            }
            if (!_entityMetaValues[metaCategory].ContainsKey(metaValue))
            {
                _entityMetaValues[metaCategory][metaValue] = new HashSet<string>();
            }
            _entityMetaValues[metaCategory][metaValue].Add(entitykey);
            if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
        }

    }
}