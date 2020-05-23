using System;
using System.Collections.Generic;
using MongoDB.Bson;
using Netlyt.Data.MongoDB;
using Netlyt.Data.SQL;

namespace Netlyt.Data
{
     /// <summary>
    /// Main class for LOCAL data source routing.
    /// </summary>
    public static class DataProvider 
    {
        public delegate IDbListBase AccessRequest(Type t);

        static DataProvider()
        {
            _dtSource = new Dictionary<Type, AccessRequest>();
        }

        private static Dictionary<Type, AccessRequest> _dtSource;
        public static void RegisterSource<T>(AccessRequest src)
        {
            if (src == null) return;
            if (_dtSource == null)
                _dtSource = new Dictionary<Type, AccessRequest>();
            _dtSource.Add(typeof (T), src);
        }

        /// <summary>
        /// Gets the DataBase List abstraction which provides access to the given type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static IDbListBase GetDatabase(Type t)
        {
            var hasSource = _dtSource.ContainsKey(t);
            if (hasSource)
            {
                AccessRequest require = _dtSource[t];
                if (require == null)
                {
                    return ProvideInternal(t);
                }
                return require(t);
            }
            else return ProvideInternal(t);
        }

        public static object RequireGeneric(Type t)
        {
            var hasSource = _dtSource.ContainsKey(t);
            if (hasSource)
            {
                AccessRequest require = _dtSource[t];
                if (require == null)
                {
                    return ProvideInternal(t);
                }
                return require(t);
            }
            else return ProvideInternalGeneric(t);
        }

        public static object Require(Type t)
        {
            var hasSource = _dtSource.ContainsKey(t);
            if (hasSource)
            {
                AccessRequest require = _dtSource[t];
                if (require == null)
                {
                    return ProvideInternal(t);
                }
                return require(t);
            }
            else return ProvideInternal(t);
        }
        /// <summary>
        /// Gets the DataBase List abstraction which provides access to the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IDbListBase<T> GetDatabase<T>()
            where T : class
        {
            return (IDbListBase<T>)GetDatabase(typeof (T));
        }

        /// <summary>
        /// Get the DataBase list abstraction from the current type register
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static IDbListBase ProvideInternal(Type t)
        {
            try
            { 
                return DBConfig.TypeBase.ContainsKey(t) ? DBConfig.TypeBase[t] : null;
            }
            catch (KeyNotFoundException ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                throw new DataSourceNotFound(t);
            }
        }

        /// <summary>
        /// Get the DataBase list abstraction from the current type register
        /// Returns IDbListBase(of t)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static object ProvideInternalGeneric(Type t)
        {
            try
            {
                //Create the generic type
                var typex = (typeof (IDbListBase<>).MakeGenericType(t)); 
                //Fetch the non generic one and cast it
                return DBConfig.TypeBase[t]?.CastObj(typex);
            }
            catch (KeyNotFoundException ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                throw new DataSourceNotFound(t);
            }
        }

        /// <summary>   Provide internal. </summary>
        ///
        /// <remarks>   Vasko, 04-Dec-17. </remarks>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        ///
        /// <returns>   A list of. </returns>

        private static IDbListBase<T> ProvideInternal<T>()
            where T : class
        {
            return (IDbListBase<T>)ProvideInternal(typeof (T));
        }

        /// <summary>   Gets a database for each of the given types. </summary>
        ///
        /// <remarks>   Vasko, 04-Dec-17. </remarks>
        ///
        /// <param name="sourceTypes">  List of types of the sources. </param>
        ///
        /// <returns>   A List&lt;IDbListBase&gt; </returns>

        public static List<IDbListBase> GetDatabasesForTypes(Type[] sourceTypes)
        {
            List<IDbListBase> output = new List<IDbListBase>();
            foreach (Type srcType in sourceTypes)
            {
                output.Add(GetDatabase(srcType)); //MakeEnumerable
            }
            return output;
        }

        public static IDbListBase GetMongoDb<T>(string collectionName)
        {
            var dbc = DBConfig.GetInstance().GetGeneralDatabase();
            var source = new MongoList(dbc.Name, collectionName, dbc.Value);
            var type = typeof(T);
            DBConfig.TypeBase.Add(type, source);
            return source;
        }
        public static IDbListBase GetMongoDb(string collectionName)
        {
            var dbc = DBConfig.GetInstance().GetGeneralDatabase();
            var source = new MongoList(dbc.Name, collectionName, dbc.Value); 
            var type = typeof(BsonDocument);
            DBConfig.TypeBase.Add(type, source);
            return source;
        }

        public static object GetSql(string tableName)
        {
            var source = new SQLList<object>();
            var type = typeof(BsonDocument);
            DBConfig.TypeBase.Add(typeof(object), source);
            return source;
        }
    }
}