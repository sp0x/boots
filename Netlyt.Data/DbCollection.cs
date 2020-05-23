using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Netlyt.Data.MongoDB;
using Netlyt.Data.SQL;

namespace Netlyt.Data
{
  /// <summary>
    /// Used to join SQLList and MongoDBList to work with id-able objects.
    /// </summary>
    /// <typeparam name="TRecord">The type of the record that will be stored</typeparam>
    /// <remarks></remarks>
    public partial class DbCollection<TRecord>
        : List<IDbListBase> where TRecord : class, new()
    {

        #region "Construction"
        /// <summary>
        /// Creates a list of databases which can be used at once.
        /// </summary>
        /// <param name="dbLists">The databases (SQL, Mongo supported)</param>
        /// <remarks></remarks>
        public DbCollection(params IDbListBase[] dbLists) : base()
        {
            if (!(dbLists==null && dbLists.Length>0))
                return;
            base.AddRange(dbLists);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="types"></param>
        public DbCollection(DatabaseConfiguration configuration, params Type[] types)
        {
            if (configuration != null)
                Console.WriteLine("Opening MongoConnection: " + configuration.Value);
            Builder bldr = new Builder(configuration.GetDatabaseName())
                .SetCollectionType(configuration.Type)
                .SetUrl(configuration.Value);
            foreach (var database in bldr.GetCollections(types))
            {
                base.Add(database);
            }
        }


        
        /// <summary>
        /// Creates a new database collection using the given type and the name, then loads a db session for each type of object.
        /// </summary>
        /// <param name="databaseType"></param>
        /// <param name="dbName"></param>
        /// <param name="types"></param>
        public DbCollection(DatabaseType databaseType, string dbName, params Type[] types) : base()
        {
            Builder bldr = new Builder(dbName).SetCollectionType(databaseType).SetUrl("");
            foreach (var database in bldr.GetCollections(types))
            {
                base.Add(database);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="databaseType"></param>
        /// <param name="types"></param>
        public DbCollection(DatabaseType databaseType, params Type[] types) :
            this(databaseType, null, types)
        {
        }
        #endregion

        #region "Public Methods"

        #region "Database object fetchers"
        public void AddDatabase(IDbListBase db)
        {
            base.Add(db);
        }
        public void AddDatabaseRange(params IDbListBase[] dbs)
        {
            foreach (var dbobj in dbs)
            { 
                try
                { base.Add(dbobj); }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Returns a dbase link of type SQLList, if it's contained.
        /// </summary> 
        /// <returns></returns>
        /// <remarks></remarks>
        public SQLList<TRecordType> MySql<TRecordType>() where TRecordType : class, new()
        {
            return (SQLList < TRecordType >)Database(typeof(SQLList<TRecordType>));
        }

        /// <summary>
        /// Returns a base link to type MongoList, if it's contained.
        /// </summary> 
        /// <returns></returns>
        /// <remarks></remarks>
        public MongoList<TRecordType> MongoDb<TRecordType>() where TRecordType : class, new()
        {
            return (MongoList < TRecordType > )Database(typeof(MongoList<TRecordType>));
        }
        public IDbListBase Database(Type ofType)
        {
            return (from x in this
                    where object.ReferenceEquals(x.GetType(), ofType)
                    select x).FirstOrDefault();
        }
         
        #endregion

        #region "Item access"
        public IDbListBase this[Type type]
        {
            get { return (from xDb in this
                          where object.ReferenceEquals(xDb.GetType(), type) 
                          select xDb).FirstOrDefault(); }
            set
            {
                dynamic dbObject = this.Where(xDb => object.ReferenceEquals(xDb.GetType(), type)).Take(1).FirstOrDefault();
                dbObject = value;
            }
        }

        #endregion


        public void Add(IDbListBase<TRecord> db)
        {
            base.Add(db);
        }
        public void AddRange(IEnumerable<IDbListBase<TRecord>> dbs)
        {
            base.AddRange(dbs);
        }

        /// <summary>
        /// Add a new element to all database links. Does not add a new database.
        /// </summary>
        /// <param name="element"></param>
        /// <remarks></remarks>
        public void Add<TSource>(TRecord element) where TSource : IDbListBase
        {
            foreach (var dbList in from db in this where db is TSource select db) { 
                dbList.Save(element);
            }
        }
        /// <summary>
        /// Adds a range of elements to all databases linked.
        /// </summary>
        /// <param name="range"></param>
        /// <remarks></remarks>
        public void AddRange<TSource>(IEnumerable<TRecord> range) where TSource : IDbListBase
        {
            foreach (var dbObject in from db in this  where db is TSource select db) { 
                dbObject.Save(range);
            }
        }




        public bool Exists<TSource>(TRecord obj) where TSource : IDbListBase
        {
            if (!Extensions.HasVal(obj))
                return true;
            //To avoid future problems 
            bool[] retArr = new bool[] {};
            foreach (var dbObject in from db in this where db is TSource select db) { 
                object reference = null;
                Arrays.Add(ref retArr, dbObject.Exists(obj, ref reference));
            }
            return  ConversionHelper.CBoolEx(retArr);
        }
        public bool Exists<TSource>(Func<TRecord, bool> key) where TSource : IDbListBase
        {
            bool[] retArr = new bool[] {};
            foreach (var dbObject in from db in this where db is TSource select db) { 
                TRecord reference = null;
                var typedObject = ((IDbListBase<TRecord>) dbObject);
                Arrays.Add(ref retArr , typedObject.Exists((x)=> key((TRecord)x), reference));
            }
            return ConversionHelper.CBoolEx(retArr);
        }

        public bool SaveOrUpdate<TSource>(TRecord elem) where TSource : IDbListBase
        {
            if (!Extensions.HasVal(elem))
                return false;
            //To avoid future problems
            bool[] retArr = new bool[3];
            foreach (var dbObject in from db in this where db is TSource select db) { 
                Arrays.Add(ref retArr, dbObject.SaveOrUpdate(elem));
            }
            return ConversionHelper.CBoolEx(retArr);
        }
        public void SaveOrUpdate<TSource>(TRecord[] elems) where TSource : IDbListBase
        {
            if (!Extensions.HasVal(elems))
                return;
            //To avoid future problems 
            foreach (IDbListBase<TRecord> dbObject in from db in this where db is TSource select db) {
                dbObject.SaveOrUpdate(elems);
            }
        }
        #endregion


        public static class Factory
        {
            //"mongodb://127.0.0.1:27017"
        }

    }
}