using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using nvoid.db.Caching;

namespace Netlyt.Service.Donut
{
    public class InternalCacheSet<T> :
        CacheSet<T> where T : class
    {
        private ICacheSetCollection _context;
        private readonly ConcurrentDictionary<string, T> _dictionary;
        private readonly List<T> _list;
        private readonly ConstructorInfo _constructor;
        private readonly object _mergeLock;
        private readonly ICacheMap<T> _cacheMap;
//        public Type ElementType { get; }
//        public Expression Expression { get; }
//        public IQueryProvider Provider { get; }

        /// <inheritdoc />
        public InternalCacheSet([NotNull] ICacheSetCollection context)
        {
            _context = context;
            _dictionary = new ConcurrentDictionary<string, T>();
            _list = new List<T>();
            _constructor = typeof(T).GetConstructor(new Type[] { });
            _mergeLock = new object();
            _cacheMap = RedisCacher.GetCacheMap<T>();
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

        public override void AddOrMerge(string key, T value)
        {
            if (Type != CacheType.Hash) throw new InvalidOperationException("CacheSet is not of type Hash");
            lock (_mergeLock)
            {
                if (!_dictionary.TryAdd(key, value))
                {
                    //Merge
                    var oldValue = _dictionary[key];
                    _cacheMap.Merge(oldValue, value);
                }
            }
            
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

        public override void Cache(CachingPersistanceService svc)
        {
            svc.Cache<T>(this); 
        }
    }
}