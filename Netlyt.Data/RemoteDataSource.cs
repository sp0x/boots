using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Netlyt.Data.MongoDB;
using Netlyt.Data.SQL;
using Newtonsoft.Json.Linq;

namespace Netlyt.Data
{
    public class RemoteDataSource<TRecord> : IRemoteDataSource<TRecord>
        where TRecord : class 
    {
        #region Vars
        private object _session = null;
        private bool _connected; 
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<Type, IDbListBase> DataFactory { get; set; } 
        private HashSet<TRecord> OfflineCache { get; set; } = new HashSet<TRecord>();

        Type IQueryable.ElementType => throw new NotImplementedException();
        /// <summary>
        ///     Gets the IQueryable LINQ Expression.
        /// </summary>
        Expression IQueryable.Expression => throw new NotImplementedException();

        /// <summary>
        ///     Gets the IQueryable provider.
        /// </summary>
        IQueryProvider IQueryable.Provider => throw new NotImplementedException();

        private IDbListBase<TRecord> mSource;
        /// <summary>
        /// The source from which the data is received, transmited
        /// </summary>
        public IDbListBase<TRecord> Source
        {
            get
            {
                if (mSource == null)
                {
                    var type = typeof(TRecord);
                    mSource = DataFactory[type] as IDbListBase<TRecord>;
                }
                return mSource;
            }
            protected set
            {
                mSource = value;
            }
        }

        public Type[] SourceTypes;
        public bool Connected => _connected;
         
        public int Count { get; private set; }
        public bool IsReadOnly { get; private set; }

        public object Session => _session;

        public int Size => Source.Size;

        #endregion

        #region Construction

        /// <summary>
        /// Creates a simple new 
        /// </summary>
        /// <param name="requireSource">If source should be figured out automatically</param>
        public RemoteDataSource(bool requireSource = false)
        {
            DataFactory = new Dictionary<Type, IDbListBase>();
            if (requireSource)
            {
                IDbListBase<TRecord> listbase = DataProvider.GetDatabase<TRecord>();
                Type target = listbase.GetType().GetGenericArguments().FirstOrDefault();
                if (target == null) throw new InvalidOperationException("DB should have a generic argument for the collection type. Example: MongoList<User>");
                this.SourceTypes = new Type []{ target };
                Connect(null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ofTypes"></param>
        public RemoteDataSource(params Type[] ofTypes)
            : this()
        {
            this.SourceTypes = ofTypes; 
            Connect(null); 
        }
        #endregion
        

        #region Methods
        /// <summary>
        /// Connect to the remote server which will provide access
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public bool Connect(Object server)//Networking.IAsyncServer server)
        {
             
            List<IDbListBase> sources = DataProvider.GetDatabasesForTypes(SourceTypes);
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                var type = source.GetType().GetGenericArguments().FirstOrDefault();
                if (type == null)
                {
                    //Type is null, the source is not a generic parameter source, get it from the SourceTypes
                    type = SourceTypes[i];
                }
                DataFactory.Add(type, source);
            }
            _connected = true;
            return true; 
        }


        /// <summary>
        /// Saves all the records in bulk in parallel.
        /// </summary>
        /// <typeparam name="TRecord"></typeparam>
        /// <param name="arts"></param>
        public void SaveAll(IEnumerable<TRecord> records)
        { 
            Source.Save(records); 
        }


//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="elem"></param>
//        /// <returns></returns>
//        public bool Save(TypedEntity elem)
//        {
//#if DEBUG
//            Source.Save((Entity)(IdAble)elem);
//#else
//            throw new NotImplementedException();
//            return null;
//#endif
//            return true;
//        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public bool Save(TRecord elem)
        { 
            this.Save((object)elem); 
            return true;
        } 

        public bool Update(TRecord entity)
        {
            return SaveOrUpdate(entity);
        }

        public bool Update(TRecord entity, Expression<Func<TRecord, bool>> filter)
        {
            return SaveOrUpdate(entity, filter);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SaveOrUpdate<T>(T data, Expression<Func<T, bool>> filter = null)
            where T : class
        { 
            var tx = typeof (T);
            if (!DataFactory.ContainsKey(tx))
            { 
                return false;
            }
            else
            {
                var source = (IDbListBase<T>)DataFactory[tx];
                var ret = source.SaveOrUpdate(data); 
                return ret;
            }
        }

        /// <summary>
        /// Updates a single entity which member`s value matches the given value.
        /// </summary>
        /// <typeparam name="TMember">The type of member the match is done against</typeparam>
        /// <param name="existingEntity">An existing object which should be updated to the given state</param>
        /// <param name="memberSelector">A member selector expression</param>
        /// <param name="value">The value to test for</param>
        /// <returns></returns>
        public bool SaveOrUpdate<TMember>(TRecord existingEntity, Expression<Func<TRecord, TMember>> memberSelector,
            TMember value) 
        {
            var tx = typeof(TRecord);
            if (!DataFactory.ContainsKey(tx))  {  return false; }
            else
            {
                var source = (IDbListBase<TRecord>)DataFactory[tx];
                var ret = source.SaveOrUpdate<TMember>(existingEntity, memberSelector, value);
                return ret;
            }
        }

        public bool SaveOrUpdate(Expression<Func<TRecord, bool>> predicate, TRecord replaceWith)
        {
            var tx = typeof(TRecord);
            if (!DataFactory.ContainsKey(tx))
            { 
                return false;
            }
            else
            {
                var source = (IDbListBase<TRecord>)DataFactory[tx];
                var ret = source.SaveOrUpdate(predicate, replaceWith);
                return ret;
            }
        }

        /// <summary>
        /// Persists that the given entity exists, wether by saving it or by updating it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Persist<T>(T data)
            where T : class
        {
            return SaveOrUpdate(data);
        }

        public void PersistAll<T>(IEnumerable<T> playlists)
            where T : class
        {
            SaveAll(playlists);
//          for (int i = 0; i < playlists.Length; i++)
//          {
//              Persist(playlists[i]);
//          }
        }

        public void SaveAll(IEnumerable coll)
        {
            if (coll == null) throw new InvalidOperationException(); 
            Type tp = coll.GetType().GenericTypeArguments.FirstOrDefault();
            if (tp == null)
            {
                tp = coll.GetType().GetElementType();
            }
            if (tp != null) DataFactory[tp].Save(coll); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IDbListBase<TRecord> GetSource() => Source as IDbListBase<TRecord>;

        #region Filtering
        public Func<object, bool> CompileFilter<TMatch>(TMatch valueTomatch)
            where TMatch : class
        {
            return this.SourceTypes.CompileFilter(valueTomatch);
        }



//        public Func<TypedEntity, bool> CompileTypedFilter<TMatch>(TMatch valueTomatch)
//         where TMatch : TypedEntity
//        {
//            return this.SourceTypes.CompileTypedFilter(valueTomatch);
//        }


        //        public Func<Entity, bool> CompileFilter(String valueTomatch)
        //        {
        //            return this.SourceTypes.CompileFilter(valueTomatch);
        //        }
        //        public Func<TypedEntity, bool> CompileTypedFilter(String valueTomatch)
        //        {
        //            return this.SourceTypes.CompileTypedFilter(valueTomatch);
        //        }
        #endregion
            
        public IEnumerator<TRecord> GetEnumerator()
        { 
            return Source.GetEnumerator<TRecord>(); 
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
//        /// <summary>
//        /// Elements are added offline!
//        /// </summary>
//        /// <param name="item"></param>
//        public void Add(TRecord item)
//        { 
//            OfflineCache.Add(item); 
//        }

        public void Trash()
        { 
            SourceTypes.ForEach(x =>
            {
                DataFactory[x].Trash();
                return true;
            }); 
        }
         

        public void Clear()
        {
            Trash();
        } 

        /// <summary>   Removes the given items. </summary>
        ///
        /// <remarks>   Vasko, 03-Dec-17. </remarks>
        ///
        /// <param name="items">    The items to remove. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>

        public bool Remove(IEnumerable<TRecord> items)
        {
            IDbListBase<TRecord> dest = DataFactory[items.GetType().GetGenericArguments().First()] as IDbListBase<TRecord>;
            dest.DeleteAll(items); 
            return true;
        }

        /// <summary>   Removes the given item from the source. </summary>
        ///
        /// <remarks>   Vasko 03-Dec-17. </remarks>
        ///
        /// <param name="item"> . </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>

        public bool Remove(TRecord item)
        { 
            var dbListBase = DataFactory[item.GetType()] as IDbListBase<TRecord>;
            return dbListBase.Delete(item); 
        }
        #endregion

        #region "Querying"

        public Cursor<TRecord> Find(Func<TRecord, bool> func, int take = 25)
        {
            var results = this.Where<TRecord>(func).Take(take);
            var cursorResult = Cursor<TRecord>.Create(results, 0);
            return cursorResult;
        }

        public TRecord FindFirst(Expression<Func<TRecord, bool>> predicate)
        {
            var source = this.GetSource();
            return source.FirstOrDefault(predicate); 
        }


        public TRecord FirstOrDefault(Expression<Func<TRecord, bool>> predicate)
        { 
            var result = FindFirst(predicate);
            if (result == null)
            {
                return default(TRecord);
            }
            else return result; 
        }


        public bool Exists<T>(T elem)
            where T : class
        { 
            object val = new object();
            return DataFactory[typeof(T)].Exists(elem, ref val); 
        }


        public bool Exists(Expression<Func<TRecord, bool>> predicate, TRecord val = null)
        { 
            var baseT = (IDbListBase<TRecord>) DataFactory[typeof (TRecord)];
            return baseT.Exists(predicate, val); 
        }


        public IEnumerable<TRecord> Where(Expression<Func<TRecord, bool>> predicate, int count = 0, int skip = 0)
        { 
            var tmpSource = ((IDbListBase<TRecord>)Source);
            return tmpSource.Where(predicate, count, skip); 
        }

        public IEnumerable<TRecord> Where(DataQuery predicate)
        { 
            var tmpSource = ((IDbListBase<TRecord>)Source);
            return tmpSource.Where(predicate); 
        } 


        public bool Contains(TRecord item)
        { 
            return ((IDbListBase<TRecord>)Source).Contains(item); 
        }
        #endregion


        /// <summary>
        /// Use DataProvider in here, to load a configured database. If not, you can also use as custom database.
        /// DB type should have a generic argument for the type the collection handles.
        /// </summary>
        /// <param name="require">The Database object</param>
        /// <returns></returns>
        public static RemoteDataSource Wrap(Type targetType, IDbListBase require)
        {
            if (require == null) return null;
            //Type target = require.GetType().GetGenericArguments().FirstOrDefault();
            //if (target == null) throw new InvalidOperationException("DB should have a generic argument for the collection type. Example: MongoList<User>");
            return new RemoteDataSource(targetType);
        }

        /// <summary>
        /// Use DataProvider in here, to load a configured database. If not, you can also use as custom database.
        /// DB type should have a generic argument for the type the collection handles.
        /// </summary>
        /// <param name="require">The Database object</param>
        /// <returns></returns>
        public static RemoteDataSource<TRecord> RequireGeneric(IDbListBase<TRecord> require)
        {
            if (require == null) return null;
            Type entityType = require.GetType().GetGenericArguments().FirstOrDefault();
            if (entityType == null) throw new InvalidOperationException("DB should have a generic argument for the collection type. Example: MongoList<User>");
            return new RemoteDataSource<TRecord>(entityType);
        }

        public Stream FetchData(FetchSource fs, string id, string bucketName = "bucket")
        {
            switch (fs)
            {
                case FetchSource.FS: 
                    var fkey = DataFactory.Keys.FirstOrDefault();
                    IMongoList mongoList = DataFactory[fkey] as IMongoList;
                    var db = (IMongoDatabase)mongoList.Database;
                    var gridfs = new GridFSBucket(db, new GridFSBucketOptions
                    {
                        BucketName = bucketName
                    });
                    return gridfs.OpenDownloadStream(id);   

            }
            return null;
        }

        /// <summary>
        /// Returns the db for mongodb
        /// </summary>
        /// <returns></returns>
        public IMongoCollection<TRecord> AsMongoDbQueryable()
        {
            var fkey = DataFactory.Keys.FirstOrDefault();
            MongoList<TRecord> db = DataFactory[fkey] as MongoList<TRecord>; 
            return db?.Records;
        }

        public SQLList<TRecord> AsSqlQueryable()
        {
            var fkey = DataFactory.Keys.FirstOrDefault();
            var database = DataFactory[fkey] as SQLList<TRecord>;
            return database;
        }

        public RemoteDataSource<TRecord> BeginTransaction()
        {
            this.AsSqlQueryable().BeginTransaction();
            return this;
        }

        public RemoteDataSource<TRecord> EndTransaction()
        {
            this.AsSqlQueryable().EndTransaction();
            return this;
        }

        public string PutData(FetchSource dest, Stream stream, string name, string bucket = "bucket")
        {
            switch (dest)
            {
                case FetchSource.FS: 
                    var fkey = DataFactory.Keys.FirstOrDefault();
                    var mongoList = DataFactory[fkey] as IMongoList;
                    var db = (IMongoDatabase)mongoList.Database;
                    var gridfs = new GridFSBucket(db, new GridFSBucketOptions
                    {
                        BucketName = bucket
                    });
                    gridfs.UploadFromStream(name, stream);
                    //return gridfs.ToString();
                    return null; 
            }
            return null;

        }

        /// <summary>
        /// Saves the entity without performing any existance checks, if not specified in a mapping or somewhere else.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Save(object entity)
        {
            Source.Save(entity);
            return true;
        }

        public bool Add(TRecord entity)
        {
            Source.Add(entity);
            return true;
        }

        public bool AddRange(IEnumerable<TRecord> entity)
        {
            Source.AddRange(entity);
            return true;
        }

        //        public bool Save<TXRecord>(Entity entity)
        //            where TXRecord : Entity
        //        {
        //#if DEBUG
        //            Source.Save<TXRecord>(entity);
        //#else
        //            throw new NotImplementedException();
        //            return null;
        //#endif
        //            return true;
        //        }

        public bool Update(object entity)
        {
            return Source.SaveOrUpdate(entity);
        }

        public bool Update(object entity, Func<object, bool> filter)
        {
            return Source.SaveOrUpdate(entity);
        }



        /// <summary>
        /// Update a single field in the record, which matches to the given selector
        /// </summary>
        /// <typeparam name="TMember"></typeparam>
        /// <param name="existingDomain"></param>
        /// <param name="memberSelector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Persist<TMember>(TRecord existingDomain, Expression<Func<TRecord, TMember>> memberSelector, TMember value)
        {
            var xSource = ((IDbListBase<TRecord>) Source);
            return xSource.SaveOrUpdate(existingDomain, memberSelector, value);
        }

        public IEnumerable<TRecord> Range(int skip, int limit)
        {
            var xSource = ((IDbListBase<TRecord>)Source);
            return xSource.Range(skip, limit);
        }

        public IEnumerable<TRecord> In<TMember>(Expression<Func<TRecord, TMember>> func, IEnumerable<TMember> values)
            where TMember : class
        {
            var xSource = ((IDbListBase<TRecord>)Source);
            return xSource.In(func, values);
        }
         
    }

    
    public class RemoteDataSource : RemoteDataSource<object>
    {
        public RemoteDataSource(Type type) : base(type)
        {
        }

        public static RemoteDataSource GetMongoDb<T>(string collectionName)
        {
            var data = DataProvider.GetMongoDb<T>(collectionName);
            var targetType = typeof(T);
            return RemoteDataSource.Wrap(targetType, data as IDbListBase);
        }
        public static RemoteDataSource GetMongoDb(string collectionName)
        {
            var data = DataProvider.GetMongoDb(collectionName);
            var targetType = typeof(BsonDocument);
            return RemoteDataSource.Wrap(targetType, data as IDbListBase);
        }
        public static RemoteDataSource GetSql(string collectionName)
        {
            var data = DataProvider.GetSql(collectionName);
            var targetType = typeof(JObject);
            return RemoteDataSource.Wrap(targetType, data as IDbListBase);
        }
    }
}