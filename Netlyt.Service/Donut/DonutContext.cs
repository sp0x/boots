using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; 
using nvoid.db.Caching;
using nvoid.db.Extensions;
using Netlyt.Service.Integration;
using StackExchange.Redis;

namespace Netlyt.Service.Donut
{
    public interface ICacheSetFinder
    { 
        IReadOnlyList<CacheSetProperty> FindSets(DonutContext context);
    }

    internal class ContextSetDiscoveryService
    {
        private DonutContext _context;
        private ICacheSetFinder _setFinder;
        private ICacheSetSource _setSource;

        public ContextSetDiscoveryService(DonutContext ctx)
        {
            _context = ctx;
            _setFinder = new CacheSetFinder();
            _setSource = new CacheSetSource();
        }

        public void Initialize()
        {
            foreach (var setInfo in _setFinder.FindSets(_context).Where(p => p.Setter != null))
            {
                setInfo.Setter.SetClrValue(_context, ((ICacheSetCollection)_context).GetOrAddSet(_setSource, setInfo.ClrType));
            }
        }
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

        /// <summary>
        /// The entity interval on which to cache the values.
        /// </summary>
        protected int CacheInterval { get; set; }
        public DonutContext(RedisCacher cacher, DataIntegration integration)
        {
            _cacher = cacher;
            CacheInterval = 100;
            Integration = integration;
            CurrentCache = new ConcurrentDictionary<string, List<HashEntry>>();
            Prefix = $"integration_context:{Integration.Id}";
            new ContextSetDiscoveryService(this).Initialize();
        }

        object ICacheSetCollection.GetOrAddSet(ICacheSetSource source, Type type)
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
//                foreach (var member in _contextMembers)
//                {
//                    CacheMember(member);
//                }
                CacheMetaContext();
            }
        }

        private void CacheMetaContext()
        {
            var meta = base.GetMetaValues();
            var entityMeta = base.GetEntityMetaValues(); 
        }

        private void CacheMember(CacheMember member)
        {
            var mValue = member.GetValue(this);
            var valType = mValue.GetType();
            //Use the member type for mapping, instead of converting to hashmap every time..
            if (typeof(IDictionary).IsAssignableFrom(valType))
            {
                var dictValueType = valType.GetGenericArguments().Skip(1).FirstOrDefault();
                if (dictValueType != null && !dictValueType.IsPrimitive)
                {
                    CacheDictionary(mValue as IDictionary, member);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if(typeof(IEnumerable).IsAssignableFrom(valType)) { 
                var dictValueType = valType.GetGenericArguments().FirstOrDefault();
                if (dictValueType != null && dictValueType.IsPrimitiveConvertable())
                {
                    CacheEnumerable(mValue as IEnumerable, member);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            //                    var cacheReadyValue = SerializeMember(member, mValue);
            //                    _cacher.SetHash(cacheKeyBase, cacheReadyValue); 
        }

        private void CacheEnumerable(IEnumerable enumerable, CacheMember member)
        {
            foreach (var element in enumerable)
            {
                var x = element;
            }
        }

        private void CacheDictionary(IDictionary dict, CacheMember member)
        {
            //Use the Type of the value, a single instance, modifying it in merges, no need to serialize
            //possibly wrap the obj in some cacheable obj interface?
            foreach (DictionaryEntry pair in dict)
            { 
                var memberKey = member.GetSubKey(pair.Key.ToString());
                ICacheSerializer serializer;
                var hash = SerializeDictionaryElement(pair.Value, out serializer);
                if (CurrentCache.ContainsKey(memberKey))
                {
                    MergeCache(CurrentCache[memberKey], hash, member, serializer);
                }
                else
                {
                    if (!CurrentCache.TryAdd(memberKey, hash))
                    {
                        throw new Exception("Cache key issue!");
                    }
                }
                
            }
        }

        private void MergeCache(List<HashEntry> oldCache, List<HashEntry> newCache, CacheMember member, ICacheSerializer serializer)
        {  
            serializer.Merge(oldCache, newCache);
            var value = newCache;
        }

       

        private List<HashEntry> SerializeDictionaryElement(object pairValue, out ICacheSerializer serializer)
        {
            var valueType = pairValue.GetType();
            serializer = RedisCacher.GetSerializer(valueType); 
            var hashEntries = serializer.Serialize(pairValue);
//            foreach (var property in valueType.GetProperties())
//            {
//                if (!property.PropertyType.IsPrimitive && !(new []{"String"}.Contains(property.PropertyType.Name))) continue;
//                object pValue = property.GetValue(pairValue) ;
//                RedisValue rv = new RedisValue(); 
//                var pHash = new HashEntry(property.Name, rv);
//                lstEntries.Add(pHash);
//            }
            return hashEntries;
        }

        private Dictionary<object, object> SerializeMember(PropertyInfo member, object mValue)
        {
            //
            throw new NotImplementedException();
        }

        private string GetMemberCacheKey(PropertyInfo member, object mValue)
        {
            var memberType = member.GetCustomAttribute<CacheKey>();
            if (memberType != null)
            {
                return mValue.ToString();
            }
            else
            {
                return null;
            } 
        } 

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cacher?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}