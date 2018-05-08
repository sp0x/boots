using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using nvoid.db.Caching;
using Netlyt.Interfaces;
using StackExchange.Redis;

namespace Donut.Caching
{
    /// <summary>
    /// TODO: Implement parallel caching for bigger caches.
    /// (current implementation uses just a few threads from the thread pool)
    /// </summary>
    public class CachingPersistеnceService
    {
        private ISetCollection _context;
        private IRedisCacher _cacher;
        public CachingPersistеnceService(ISetCollection ctx)
        {
            _context = ctx;
            _cacher = ctx.Database;
        }

        /// <summary>
        /// Persists a CacheSet object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheSet"></param>
        public async Task Cache<T>(ICacheSet<T> cacheSet, ICacheMap<T> cacheMap) 
            where T : class
        {
            var setKey = cacheSet.Name;
            switch (cacheSet.Type)
            {
                case CacheType.Set:
                    var cacheSetElements = cacheSet.GetSet();
                    CacheSetElements(setKey, cacheSetElements, cacheMap);
                    break;
                case CacheType.Hash:
                    var cachedHashes = cacheSet.GetHashes();
                    await CacheHashes(setKey, cachedHashes, cacheMap);
                    break;
                default:
                    throw new NotImplementedException("Unsupported cache type!");
            }
//            var mValue = member.GetValue(this);
//            var valType = mValue.GetType();
//            //Use the member type for mapping, instead of converting to hashmap every time..
//            if (typeof(IDictionary).IsAssignableFrom(valType))
//            {
//                var dictValueType = valType.GetGenericArguments().Skip(1).FirstOrDefault();
//                if (dictValueType != null && !dictValueType.IsPrimitive)
//                {
//                    CacheDictionary(mValue as IDictionary, member);
//                }
//                else
//                {
//                    throw new NotImplementedException();
//                }
//            }
//            else if (typeof(IEnumerable).IsAssignableFrom(valType))
//            {
//                var dictValueType = valType.GetGenericArguments().FirstOrDefault();
//                if (dictValueType != null && dictValueType.IsPrimitiveConvertable())
//                {
//                    CacheEnumerable(mValue as IEnumerable, member);
//                }
//                else
//                {
//                    throw new NotImplementedException();
//                }
//            }
            //                    var cacheReadyValue = SerializeMember(member, mValue);
            //                    _cacher.SetHash(cacheKeyBase, cacheReadyValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="hashtable"></param>
        /// <param name="cacheMap"></param>
        private async Task CacheHashes<T>(string hashKey, IDictionary<string, T> hashtable, ICacheMap<T> cacheMap)
            where T : class
        { 
            var prefix = $"{_context.Prefix}:{hashKey}"; 
            foreach (var pair in hashtable)
            {
                var memberKey = cacheMap.GetKey(prefix, pair.Key); 
                var hashElements = SerializeDictionaryElement(pair.Value, cacheMap);
                await _cacher.SetHashesAsync(memberKey, hashElements);
            }
            //var elapsed2 = watch2.ElapsedMilliseconds; Debug.WriteLine($"HASH Cache time: {elapsed2}ms {msSum}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setKey"></param>
        /// <param name="cacheSetElements"></param>
        /// <param name="cacheMap"></param>
        private void CacheSetElements<T>(string setKey,IEnumerable<T> cacheSetElements, ICacheMap<T> cacheMap)
            where T : class
        {
            var key = cacheMap.GetKey(_context.Prefix, setKey); 
            //var memberKey = member.GetSubKey(pair.Key.ToString()); 
            var serializedValues = SerializeSet(cacheSetElements, cacheMap);
            foreach (var sv in serializedValues)
            {
                _cacher.SetAdd(key, sv);
            }
        }  

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        private IEnumerable<RedisValue> SerializeSet<T>(IEnumerable<T> values, ICacheMap<T> map)
            where T : class
        {
            foreach (var val in values)
            {
                if (val == null) continue;
                RedisValue rv = map.SerializeValue(val);//val as RedisValue;
                yield return rv; 
            }
        }

        private IEnumerable<HashEntry> SerializeDictionaryElement<T>(T pairValue, ICacheMap<T> map)
            where T : class
        {
            var hashEntries = map.Serialize(pairValue);
            return hashEntries;
        }

        public T GetHash<T>(string key, ICacheMap<T> map)
            where T : class
        {
            var ret = _cacher.GetHash(key);
            var element = map.DeserializeHash(ret);
            return element;
        }
        /// <summary>
        /// Gets the size of a set
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long GetSetSize(string key)
        {
            return _cacher.GetSetItemCount(key);
        }

        public void Truncate(string key)
        {
            _cacher.RemoveAll($"{key}:*");
            _cacher.RemoveAll($"{key}");
        }

        public int AddHashWithIndex(string prefix, Dictionary<string, object> aggKeyBuff)
        {
            var key = $"{prefix}:cacheGroup";
            throw new NotImplementedException();
        }
    }
}