using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using nvoid.db.Caching;
using Netlyt.Interfaces;

namespace Netlyt.Service.Donut
{
    public class InternalCacheSet<T> : CacheSet<T> where T : class
    {
        private ISetCollection _context;
        private readonly ConcurrentDictionary<string, T> _dictionary;
        private readonly HashSet<T> _list;
        private readonly ConstructorInfo _constructor;
        private readonly object _mergeLock;
        private readonly ICacheMap<T> _cacheMap;
        private CachingPersistеnceService _cachingService;

        /// <inheritdoc />
        public InternalCacheSet([NotNull] ISetCollection context)
        {
            _context = context;
            _dictionary = new ConcurrentDictionary<string, T>();
            _list = new HashSet<T>();
            _constructor = typeof(T).GetConstructor(new Type[] { });
            _mergeLock = new object();
            _cacheMap = RedisCacher.GetCacheMap<T>();
            _cachingService = new CachingPersistеnceService(_context);
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

//        IEnumerator IEnumerable.GetEnumerator()
//        {
//            return GetEnumerator();
//        }


        public override bool ContainsKey(string key)
        {
            if (Type != CacheType.Hash) return false;
            return _dictionary.ContainsKey(key);
        }

        public override void Add(string key, T value)
        {
            if (Type != CacheType.Hash) throw new InvalidOperationException("CacheSet is not of type Hash");
            if (!_dictionary.TryAdd(key, value))
            {
                throw new Exception("Could not add element");
            }
        }

        /// <summary>
        /// Gets or adds an element with the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override T GetOrAddHash(string key)
        {
            if (Type != CacheType.Hash) return default(T);
            var element = _dictionary.GetOrAdd(key, CreateEntity(key));
            return element;
        }

        public override IEnumerable<T> GetSet()
        {
            return _list;
        }

        public override IDictionary<string, T> GetHashes()
        {
            return _dictionary;
        }

        public override T AddOrMerge(string key, T value)
        {
            if (Type != CacheType.Hash) throw new InvalidOperationException("CacheSet is not of type Hash");
            T outputValue = default(T);
            lock (_mergeLock)
            {
                T oldValue = null;
                if (!_dictionary.ContainsKey(key))
                {
                    var fqkey = _cacheMap.GetKey(_context.Prefix, Name, key);
                    var hashValue = _cachingService.GetHash(fqkey, _cacheMap);
                    if (hashValue != null)
                    {
                        _dictionary.TryAdd(key, hashValue);
                        oldValue = hashValue;
                    }
                }
                else oldValue = _dictionary[key];
                if (oldValue != null) value = _cacheMap.Merge(oldValue, value);
                _dictionary[key] = value;
                outputValue = value;
            }
            return outputValue;
        }

        public override long Count()
        {
            return _cachingService.Count(Name);
        }

        public override void Truncate()
        {
            var keyBase = _cacheMap.GetKey(_context.Prefix, Name); 
            _cachingService.Truncate(keyBase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private T CreateEntity(string key)
        {
            return _constructor.Invoke(new object[] { }) as T;
        }

        public override void Add(T element)
        {
            if (Type != CacheType.Set) throw new InvalidOperationException("CacheSet is not of type Set");

            _list.Add(element);
        }

        public override void SetType(CacheType backingType)
        {
            Type = backingType;
        }

        public override void Cache()
        {
            CacheAsync().Wait(); 
        }

        public override async Task CacheAsync()
        {
            await _cachingService.Cache(this, _cacheMap);
        }
        public override void ClearLocalCache()
        {
            _dictionary.Clear();
            _list.Clear();
        }
    }
}