using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using Expression = NHibernate.Criterion.Expression;

namespace Netlyt.Data.SQL
{
    public class QueryExpressionCompiler<T> : QueryExpressionCompiler
    {

        public QueryExpressionCompiler(List<QueryParameter> parameters) : base(parameters)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Expression<Func<T,bool>> GetExpression(T entity)
        {  
            ParameterExpression recordParameter = Expression.Parameter(typeof(T), "record"); 
            BinaryExpression lastExpression = null;
            foreach (var p in Parameters)
            {
                var value = p.GetValue(entity);
                var right = Expression.Constant(value);
                var left = Expression.Property(recordParameter, p.Member);
                var crExpression = Expression.Equal(left, right);
                if (lastExpression == null) lastExpression = crExpression;
                else lastExpression = Expression.AndAlso(lastExpression, crExpression);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(lastExpression, recordParameter);
            return lambda;
        }

    }
    
    /// <summary>   A query provider that resolves the best query for  entites, based on annotation or  key properties. </summary>
    ///
    /// <remarks>   Vasko, 15-Dec-17. </remarks>

    public class DbQueryProvider
        : IDbQueryProvider
    {
        private readonly Object _cacheLock = new Object();
        private static ConcurrentDictionary<string, QueryExpressionCompiler> QueryCache { get; set; }
        private static DbQueryProvider _instance;

        public DbQueryProvider()
        {
            lock (_cacheLock)
            {
                if (QueryCache == null)
                {
                    QueryCache = new ConcurrentDictionary<string, QueryExpressionCompiler>();
                }
            }

        }

        public static DbQueryProvider GetInstance()
        {
            if (_instance == null)
            {
                _instance = new DbQueryProvider();
            }
            return _instance;
        }

        /// <summary>   Gets unique query for the given entity. 
        ///             Note, this uses the default database. </summary>
        ///
        /// <remarks>   Vasko, 15-Dec-17. </remarks>
        ///
        /// <typeparam name="TRecord">  Type of the record. </typeparam>
        /// <param name="entity">   The entity. </param>
        ///
        /// <returns>   The unique query. </returns>

        public Expression<Func<TRecord, bool>> GetUniqueQuery<TRecord>(TRecord entity) where TRecord : class
        {
            IDbListBase<TRecord> dbBase = DataProvider.GetDatabase<TRecord>();
            return GetUniqueQuery<TRecord>(dbBase, entity);
        }

        public Expression<Func<object,bool>> GetUniqueQuery(object entity)
        {
            var dataType = entity.GetType();
            IDbListBase dbBase = DataProvider.GetDatabase(dataType);
            return GetUniqueQuery<object>(dbBase as IDbListBase<object>, entity);
        }

        public Expression<Func<TRecord, bool>> GetUniqueQuery<TRecord>(IDbListBase<TRecord> source, TRecord entity) where TRecord : class
        {
            //FIgure out if there`s a cached query existing
            var cachedQuery = GetCachedQuery<TRecord>(entity);
            if (cachedQuery != null)
            {
                return cachedQuery;
            }
            ParameterExpression recordParameter =
                Expression.Parameter(typeof(TRecord), "record");
            var baseType = typeof(TRecord);
            var indexes = source.GetIndexes();
            //Fetch index keys for the given type 
            var indexKeys = indexes
                .Where(x => x.Unique || x.Name=="_id_")
                .SelectMany(x => x.Keys);
            BinaryExpression lastExpression = null;
            var parameters = new List<QueryParameter>();
            foreach (var indexKey in indexKeys)
            { 
                string propName = indexKey.Name;
                if (propName == "_id") propName = "Id";
                var member = baseType.GetProperty(propName);
                if (member == null) continue;
                var memberValue = member.GetValue(entity);
                var left = Expression.Property(recordParameter, member);
                var right = Expression.Constant(memberValue);
                var queryParam = new QueryParameter(left, member);
                parameters.Add(queryParam);
                var crExpression = Expression.Equal(left, right);
                if (lastExpression == null) lastExpression = crExpression;
                else lastExpression = Expression.AndAlso(lastExpression, crExpression);
            }
            var lambda = Expression.Lambda<Func<TRecord, bool>>(lastExpression, recordParameter);
            //Todo: maybe try the Id property if present
            CacheQuery<TRecord>(new QueryExpressionCompiler<TRecord>(parameters));//new QueryExpressionBuilder());
            return lambda;
        }

        public Expression<Func<TRecord, bool>> GetUniqueQueryForProperty<TRecord>(IRemoteDataSource<TRecord> source, string uniqueIndexName, TRecord value,
            bool ignoreNullValues = false) where TRecord : class
        {
            PropertyInfo property = typeof(TRecord).GetProperty(uniqueIndexName);
            if (property == null)
            {
                property = MongoIndexed.GetMemberByElementKey<TRecord>(uniqueIndexName);
            }
            if (property == null)
            {
                Trace.WriteLine(string.Format("WARNING: element {0} from unique MongoDb Index doesn't exist in the type {1}.", uniqueIndexName, typeof(TRecord)));
                return null;
            }
            ParameterExpression recordParameter =
                Expression.Parameter(typeof(TRecord), "record");
            var propertyValue = property.GetValue(value);
            var left = Expression.Property(recordParameter, property);
            var right = Expression.Constant(propertyValue);
            var e1 = Expression.Equal(left, right);
            var lambda = Expression.Lambda<Func<TRecord, bool>>(e1, recordParameter);
            return lambda;

            //            BsonValue bsvalue = null;
            //            if (!DBExtensions.TryCreateBsonValue(propertyValue, ref bsvalue))
            //            {
            //                if (propertyValue != null)
            //                {
            //                    //Map the property and all of it's subprops which are registered, map them all as extensions of the base of the current object 
            //                    //newQr = GetTypeMemberQueries(uniqueIndexName, tmpValue.ToString());
            //                    newQr = GetTypeMemberQueries<TRecord>(uniqueIndexName, propertyValue.ToString());
            //                }
            //                else
            //                {
            //                    newQr = Builders<TRecord>.Filter.Eq(uniqueIndexName, BsonNull.Value);
            //                }
            //
            //            }
            //            else
            //            {
            //                newQr = Builders<TRecord>.Filter.Eq(uniqueIndexName, bsvalue);
            //            }
            //            return newQr;
        }

        public Expression<Func<TRecord, bool>> GetCachedQuery<TRecord>(object value, string prefix = null)
            where TRecord : class
        {
            return GetCachedQuery<TRecord>(GetCacheKey(value.GetType()), value, prefix);
        }

        public Expression<Func<TRecord, bool>> GetCachedQuery<TRecord>(string key, object value, string prefix)
            where TRecord : class
        { 
            if (QueryCache.ContainsKey(key))
            {
                var exp = QueryCache[key] as QueryExpressionCompiler<TRecord>;
                return exp?.GetExpression(value as TRecord);
                //                if ((qrObj = (mongoPreparedQuery.Build(value, prefix))) != null)
                //                {
                //                    return qrObj;
                //                }
            }
            return null;
        }

        /// <summary>   Gets a key member lambda expression. </summary>
        ///
        /// <remarks>   Vasko, 15-Dec-17. </remarks>
        ///
        /// <typeparam name="TRecord">  Type of the record. </typeparam>
        ///
        /// <returns>   The key member expression. </returns>

        public LambdaExpression GetKeyMemberExpression<TRecord>(out PropertyInfo keyMember)
        {
            var idMember = GetKeyMember<TRecord>();
            var expParam = Expression.Parameter(typeof(TRecord), "record");
            var expMember = Expression.Property(expParam, (PropertyInfo)idMember);
            var delegateType = typeof(Func<>).MakeGenericType(typeof(TRecord), ((PropertyInfo)idMember).PropertyType);
            var lambda = Expression.Lambda(delegateType, expMember, expParam);
            keyMember = idMember as PropertyInfo;
            return lambda;
        }

        public PropertyInfo GetKeyMember<TRecord>()
        {
            return GetKeyMember(typeof(TRecord));
        }

        public PropertyInfo GetKeyMember(Type type)
        {
            if (type == null) return null;
            var idMember = type.GetMember("Id").FirstOrDefault();
            return idMember as PropertyInfo;
        }

        public IEnumerable<object> GetKeyMemberValues<TRecord>(IEnumerable<TRecord> elements, out PropertyInfo keyMember)
        { 
            var accessor = GetKeyMemberExpression<TRecord>(out keyMember);
            var query = elements.AsQueryable().Provider.CreateQuery(accessor);
            return query.Cast<object>();
        }
         

        /// <summary>   Gets type member queries. </summary>
        ///
        /// <remarks>   vasko, 14-Dec-17. </remarks>
        ///
        /// <typeparam name="TRecord">  Type of the record. </typeparam>
        /// <param name="valueToMatch">         The element. </param>
        /// <param name="elementName">  (Optional) Name of the element. </param>
        ///
        /// <returns>   The type member queries. </returns>

        public Expression<Func<TRecord, bool>> GetMemberQuery<TRecord>(object valueToMatch, string elementName = null)
            where TRecord : class
        {
            if (valueToMatch == null) return null; 

            string memberPrefix = string.Empty;
            if (!string.IsNullOrEmpty(elementName))
            {
                memberPrefix = string.Format("{0}.", elementName);
            }
            //Check if a cached query for the document exists.
            string cachedKey = GetCacheKey(valueToMatch.GetType());
            Expression<Func<TRecord, bool>> outputQuery = GetCachedQuery<TRecord>(cachedKey, valueToMatch, memberPrefix);
            if (outputQuery != null) return outputQuery;
            else
                outputQuery = null;

            //Get members and their maps
            IEnumerable<BsonMemberMap> allowedTypes = BsonClassMap.LookupClassMap(valueToMatch.GetType()).AllMemberMaps;
            var properties = valueToMatch.GetType().GetProperties();
            var membersStack = new Stack<MemberInfo>();
            ParameterExpression recordParameter =
                Expression.Parameter(valueToMatch.GetType(), "record");
            BinaryExpression lastComparison = null;
            var parameters = new List<QueryParameter>();
            membersStack.WalkNonRecursive<MemberInfo>(
                properties,
                false,
                (ref MemberInfo propMember, ref bool isValid) =>
                {
                    PropertyInfo propTarget = (PropertyInfo)propMember;
                    if (allowedTypes.FirstOrDefault(sMap => sMap.ElementName == propTarget.Name) == null)
                    {
                        return true;
                    }
                    var propValue = propTarget.GetValue(valueToMatch);
                    //Build an expression for parameter {memberPrefix}{propMember.Name}
                    // matching propValue
                    var left = Expression.Property(recordParameter, propTarget);
                    parameters.Add(new QueryParameter(left, propTarget));
                    var right = Expression.Constant(propValue);
                    var e1 = Expression.Equal(left, right);
                    if (lastComparison == null)
                    {
                        lastComparison = e1;
                    }
                    else
                    {
                        lastComparison = Expression.AndAlso(lastComparison, e1);
                    }
                    isValid = true;
                    //                    if (DBExtensions.TryCreateBsonValue(propValue, ref tmpBsonVal))
                    //                    {
                    //                        var query = Builders<TRecord>.Filter.Eq($"{memberPrefix}{propMember.Name}", tmpBsonVal);
                    //                        outputQuery = Builders<TRecord>.Filter.And(outputQuery, query);
                    //                        
                    //                    }
                    //                    else
                    //                    {
                    //                        return true;
                    //                        // Push all property members, because it's not a scalar/string value
                    //                    }
                    return true;
                },
                (MemberInfo x) =>
                {
                    PropertyInfo propTarget = (PropertyInfo)x;
                    if (propTarget.PropertyType.Assembly.FullName.Contains("mscorlib,"))
                    {
                        return null;
                    }
                    else
                    {
                        return x.DeclaringType.GetProperties();
                    }
                }
            );
            outputQuery = Expression.Lambda<Func<TRecord, bool>>(lastComparison, recordParameter);

            CacheQuery(cachedKey, new QueryExpressionCompiler<TRecord>(parameters));
            return outputQuery;
        }


        private void CacheQuery<TRecord>(QueryExpressionCompiler<TRecord> value)
        {
            CacheQuery(GetCacheKey(typeof(TRecord)), value);
        }
        private void CacheQuery(string key, QueryExpressionCompiler value)
        {
            if (value != null)
            {
                QueryCache.TryAdd(key, value);
            }

        }

        private static string GetCacheKey(Type type)
        {
            string cachedKey = type.ToString();
            return cachedKey;
        }
        private static string GetCacheKey<T>()
        {
            return GetCacheKey(typeof(T));
        }

    }
}