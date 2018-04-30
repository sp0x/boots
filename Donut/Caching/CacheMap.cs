using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using nvoid.db.Caching;
using StackExchange.Redis;

namespace Donut.Caching
{
    /// <summary>
    /// Used to add details to how an entity's cache is used.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CacheMap<T> : ICacheMap<T>
        where T : class
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, Func<T, RedisValue>> Convertors { get; set; }
        /// <summary>
        /// Definitions of actions used to merge entity cache.
        /// </summary>
        private Dictionary<string, Action<T, T>> CacheMergers { get; set; }
        private Dictionary<string, CacheMember> Members { get; set; }
        private Dictionary<string, Func<RedisValue, object>> CacheDeserializers { get; set; }

        private Func<T, RedisValue> _primitiveConvertor;
        private bool _isPrimitive = false;
        private ConstructorInfo _constructor;

        public CacheMap()
        {
            Members = new Dictionary<string, CacheMember>();
            Convertors = new Dictionary<string, Func<T, RedisValue>>();
            CacheMergers = new Dictionary<string, Action<T, T>>();
            CacheDeserializers = new Dictionary<string, Func<RedisValue, object>>();
            var tType = typeof(T);
            if (tType.IsPrimitive || tType.Name == "String")
            {
                if (tType.Name == "String") _primitiveConvertor = (T x) => (RedisValue)(x as string);
                _isPrimitive = true;
            }
            else
            {
                _constructor = typeof(T).GetConstructor(new Type[] { });
                if (_constructor == null)
                    throw new InvalidOperationException(
                        $"Make sure {typeof(T).FullName} has a default parameterless constructor!");
            }
        }

        public abstract void Map();

        public CacheMapRule<T> AddMember(Expression<Func<T, RedisValue>> fetcher, string key = null)
        {
            var unaryExpression = (UnaryExpression)fetcher.Body;
            while (unaryExpression.Operand.NodeType == ExpressionType.Convert) //Strip casts
            {
                unaryExpression = (UnaryExpression) unaryExpression.Operand;
            }
            var memberExp = (MemberExpression)unaryExpression.Operand;
            var accessor = fetcher.Compile();
            var prefixes = new List<string>();
            MemberInfo rootMember = null;
            if (memberExp.Expression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression subExpression = (MemberExpression)memberExp.Expression;
                do
                {
                    if (rootMember == null) rootMember = subExpression.Member;
                    prefixes.Add(subExpression.Member.Name);
                    if (subExpression.Expression.NodeType != ExpressionType.MemberAccess) break;
                    subExpression = (MemberExpression)subExpression.Expression;
                } while (true);
            }
            if (rootMember == null) rootMember = memberExp.Member;
            //            string strPrefix = string.Join(".", prefixes.ToArray());
            //            if (strPrefix.Length > 0) strPrefix += ".";
            //var cMember = new CacheMember(strPrefix, (PropertyInfo)memberExp.Member);
            var cMember = new CacheMember("", (PropertyInfo)rootMember);
            if (!string.IsNullOrEmpty(key)) cMember.SetKey(key);
            Convertors.Add(cMember.Key, accessor);
            Members[cMember.Key] = cMember;
            var newRule = new CacheMapRule<T>(this, cMember, fetcher);
            return newRule;
        }


        public T DeserializeHash(HashEntry[] hashMembers)
        {
            if (hashMembers == null || hashMembers.Length == 0) return default(T);
            var element = _constructor.Invoke(new object[] { }) as T;
            foreach (var hashMember in hashMembers)
            {
                var member = Members[hashMember.Name];
                var memberProperty = member.Property;
                if (CacheDeserializers.ContainsKey(member.Key))
                {
                    var deserializer = CacheDeserializers[member.Key]; 
                    var value = deserializer(hashMember.Value);
                    memberProperty.SetValue(element, value);
                }
                else
                {
                    switch (memberProperty.PropertyType.Name)
                    {
                        case "String": memberProperty.SetValue(element, (string)hashMember.Value); break;
                        case "Byte": memberProperty.SetValue(element, (byte)hashMember.Value); break;
                        case "Int32": memberProperty.SetValue(element, (int)hashMember.Value); break;
                        case "Int64": memberProperty.SetValue(element, (long)hashMember.Value); break;
                        case "Int16": memberProperty.SetValue(element, (short)hashMember.Value); break;
                        case "UInt16": memberProperty.SetValue(element, (ushort)hashMember.Value); break;
                        case "UInt32": memberProperty.SetValue(element, (uint)hashMember.Value); break;
                        case "UInt64": memberProperty.SetValue(element, (ulong)hashMember.Value); break;
                        case "Double":
                            memberProperty.SetValue(element, (double)hashMember.Value);
                            break;
                        case "Float": memberProperty.SetValue(element, (float)hashMember.Value); break;
                        case "Decimal": memberProperty.SetValue(element, (decimal)hashMember.Value); break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                

            }
            return element;
        }
        public List<HashEntry> ToHash(object o)
        {
            var hashlist = new List<HashEntry>(Convertors.Count);
            try
            {
                var objT = (T)o;
                if (objT == null) return null;
                foreach (var convertorPair in Convertors)
                {
                    try
                    {
                        var cValue = convertorPair.Value(objT);
                        var member = Members[convertorPair.Key];
                        var cHash = new HashEntry(member.GetName(), cValue);
                        hashlist.Add(cHash);
                    }
                    catch (Exception ex2)
                    {
#if DEBUG
                        Debug.WriteLine(ex2.Message);
#endif
                    }
                }
            }
            catch
            {
                return null;
            }
            return hashlist;

        }

        /// <summary>
        /// Merges the new cache into the old one.
        /// </summary>
        /// <param name="oldCache"></param>
        /// <param name="newCache"></param>
        /// <returns></returns>
        public T Merge(T oldCache, T newCache)
        {
            bool merged = false;
            foreach (var pair in CacheMergers)
            {
                var merger = pair.Value;
                merger.Invoke(oldCache, newCache);
                merged = true;
            }
            return merged ? oldCache : newCache;
        }

        public string GetKey(params string[] segments)
        {
            var enumerable = segments.Where(x => x.Length > 0).ToArray();
            return String.Join(":", enumerable);
        }

        public RedisValue SerializeValue(T val)
        {
            if (_isPrimitive) return _primitiveConvertor(val);
            else
            {
                throw new NotImplementedException("Not supported yet!");
            }
        }


        public void Merge(ref HashEntry oldCache, HashEntry newCache)
        {
            var newVal = oldCache.Value + newCache.Value;
            oldCache = new HashEntry(oldCache.Name, newVal);
        }

        public Action<T, T> GetMerger(CacheMember member)
        {
            if (CacheMergers.ContainsKey(member.Key)) return CacheMergers[member.Key];
            else return null;
        }

        public void MergeBy(CacheMember member, Action<T, T> merger)
        {
            CacheMergers[member.Key] = merger;
        }

        public void AddDeserializer(CacheMember cMember, Func<RedisValue, object> func)
        {
            CacheDeserializers[cMember.Key] = func;
        }
    }

    public static class Extensions
    {
        public static List<HashEntry> Serialize(this ICacheMap map, object value)
        {
            var res = map.ToHash(value);
            return res;
        }
    }
}