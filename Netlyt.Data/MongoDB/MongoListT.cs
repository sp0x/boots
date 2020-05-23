using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Donut.Batching;
using Donut.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Netlyt.Data.SQL;

namespace Netlyt.Data.MongoDB
{
    public partial class MongoList<TRecord>
       : DBBase<TRecord>, IMongoList
       where TRecord : class
    {
        private static bool _hasMappedClasses = false;
        /// <summary>
        /// A cache for indexes.
        /// </summary>
        private List<Index> _indexes;
        /// <summary>
        /// Key must be: TypeName + ElementName
        /// </summary>
        /// <remarks></remarks>

        #region "Mapping helpers"
        public static object TryRegisterClass<TClass>(Action<BsonClassMap<TClass>> initializer = null)
        {
            try
            {
                if (!BsonClassMap.IsClassMapRegistered(typeof(TClass)))
                {
                    if (initializer == null)
                    {
                        initializer = cm =>
                        {
                            cm.AutoMap();
                            cm.SetIgnoreExtraElements(true);
                        };
                    }
                    return BsonClassMap.RegisterClassMap<TClass>(initializer) != null;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion


        #region "Construction"
        public static void RegisterMaps()
        {
            if (_hasMappedClasses)
                return;
            TryRegisterClass<object>(cm =>
            {
                cm.AutoMap();
                //cm.GetMemberMap(mbr => mbr.Id).SetRepresentation(BsonType.ObjectId);
                cm.SetIgnoreExtraElements(true);
            });
            SetupMapping();
            _hasMappedClasses = true;
        }


        private static void SetupMapping()
        {
            //How you get this assembly is up to you
            //It could be this assembly
            //Or it could be a collection of EntryCollection, in which case, wrap this block in a foreach and iterate
            HashSet<AssemblyWrapper> asmRefs = Extensions.GetProjectReferences(new PersistanceSettings());
            asmRefs = asmRefs.GetAssembliesToMap(MongoUtils.IncludedAssemblies);
            foreach (var asm in asmRefs)
            {
                MongoUtils.AddMapedAssembly(asm.Assembly);
            }
        }

        #region "Construction"
        public MongoList()
        {
            TryRegisterClass<TRecord>();
        }

        /// <summary>
        /// Creates a new mongodb list, and uses the type of T as a collection name by default.
        /// </summary>
        /// <param name="dbnm"></param>
        /// <remarks></remarks>
        public MongoList(string dbnm) : this(dbnm, typeof(TRecord).Name)
        {
        }
         
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbnm">The name of the database to use</param>
        /// <param name="collection">The name of the collection to use</param>
        /// <param name="url">A connection url for mongodb</param> 
        public MongoList(string dbnm, string collection, string url = null)
        {
            if (String.IsNullOrEmpty(url)) url = DBConfig.DefaultMongoHost;
#if DEBUG
            Debug.WriteLine($"Connecting to mongo : {url}");
#endif
            DbName = dbnm; 
            var murl = (new MongoUrlBuilder(url)
            {
                ConnectTimeout = TimeSpan.FromSeconds(10),
                ServerSelectionTimeout = TimeSpan.FromSeconds(10),
                SocketTimeout = TimeSpan.FromSeconds(10),
                WaitQueueTimeout = TimeSpan.FromSeconds(10),
                AuthenticationSource = "admin"
            }).ToMongoUrl(); 
            Connection = new MongoClient(murl);
            Database = Connection.GetDatabase(DbName);
            RegisterMaps();
            try
            {
                if (!CollectionExistsAsync(Database, collection).Result)
                {
                    Database.CreateCollection(collection);
                } 
            }
            catch (TimeoutException e){
                throw e;
            }
            Records = Database.GetCollection<TRecord>(collection);
            //IndexType<TRecord>(useGeospatial, geoindex);
            TryRegisterClass<TRecord>();
        }
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Vasko, 04-Dec-17. </remarks>
        ///
        /// <param name="dbc">          The database configuration. </param>
        /// <param name="collection">   The name of the collection to use. </param>

        public MongoList(DatabaseConfiguration dbc, string collection)
        {
            Connection = new MongoClient(dbc.Value);
            Database = Connection.GetDatabase(dbc.GetDatabaseName());
            if (null == (Records = Database.GetCollection<TRecord>(collection)))
                Database.CreateCollection(collection);
            Records = Database.GetCollection<TRecord>(collection);
        }
         

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="isGeospation"></param>
        public MongoList<TRecord> SetIndex(string index, bool isGeospation = false)
        {
            IndexType(isGeospation, index);
            return this;
        }


        /// <summary>
        /// Builds the needed indexes for TRecordType, and applies them to the collection.
        /// </summary>
        /// <typeparam name="TRecordType"></typeparam>
        /// <param name="useGeospatial"></param>
        /// <param name="geoIndex"></param>
        private void IndexType(bool useGeospatial = false, string geoIndex = null)
        {
            //Build all indexes which are set as attributes
            foreach (MongoIndex<TRecord> index1 in MongoAutoIndexer.BuildIndexes<TRecord>())
            {
                index1.TryApply(mRecords);
            }
            if (geoIndex!=null && geoIndex.Length>0 & useGeospatial)
            {
                var gindex = Builders<TRecord>.IndexKeys.Geo2D(geoIndex);
                Records.Indexes.CreateOne(geoIndex);
            }
        }

        #endregion
        #endregion



        #region "Vars"
        public string DbName { get; set; }
        public MongoClient Connection { get; set; }
        public string URL { get; set; }
        //private MongoServer _server { get; set; }

        public IMongoDatabase Database { get; private set; }

        private IMongoCollection<TRecord> mRecords;
        public IMongoCollection<TRecord> Records
        {
            get { return mRecords; }
            set { mRecords = value; }
        }
        public string GeospaceIndex { get; set; }
        #endregion

        #region "Props"
//        public MongoServer Server
//        {
//            get
//            {
//                if (_server != null)
//                    return _server;
//                if (Connection == null)
//                    return null;
//                else { _server = Connection.GetServer(); return _server; }
//            }
//        }
        public override bool Connected
        {
            get
            {
                if (Connection == null | Records == null)
                    return false;
                //Throw New IPGeoDBConnectionException("Cannot connect to mongodb server " & URL & "!")
                return true;
            }
        }
        public override int Size
        {
            get
            {
                if (!Connected)
                    return -1;
                return (int)Records.Count(FilterDefinition<TRecord>.Empty);
            }
        }

        public override string CollectionName { get; }

        public IQueryable<TRecord> QueryableCollection1
        {
            get
            {
                if (Records == null)
                    return null;
                return Records.AsQueryable<TRecord>();
            }
        }



        public override IQueryable AsQueryable => Records?.AsQueryable<TRecord>();
        public override IQueryable<TRecord> AsQueryable1 => Records?.AsQueryable<TRecord>();

        #endregion

        #region "Insert functions"
        /// <summary>
        /// Insert a Single entity.
        /// If you wanna insert several entities in a row, please use InsertBatch because it is faster for multiple insertion.
        /// </summary>
        /// <param name="entity">Entity name</param>
        public override void Add(TRecord entity)
        {
            if (entity != null)
            {
                try
                {
                    Records.InsertOne(entity); 
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format(entity.GetType().Name, ex.Message), "Fatal error: "); 
                }
            } 
        }

        /// <summary>
        /// Insert Single entity.
        /// If you wanna insert several entities in a row, please use InsertBatch because it is faster for multiple insertion.
        /// </summary>
        /// <param name="entity">Entity name</param>
        public void Add(BsonDocument entity)
        {
            if (entity != null)
            {
                try
                {
                    // Dim ent As t = Bson.Serialization.BsonSerializer.Deserialize(Of t)(entity)
                    var ent = BsonSerializer.Deserialize<TRecord>(entity);
                    Records.InsertOne(ent);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Insert in batch
        /// </summary>
        /// <param name="entities">ICollection of Entities of T typeof</param>
        public bool AddRange(ICollection<TRecord> entities)
        {
            if (entities != null)
            {
                Records.InsertMany(entities);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Insert in batch
        /// </summary>
        /// <param name="entities">ICollection of Entities of T typeof</param>
        public bool AddRange(ICollection<BsonDocument> entities)
        {
            if (entities != null)
            {
                var items = entities.Select(x=>BsonSerializer.Deserialize<TRecord>(x));
                Records.InsertMany(items);
            }
            return false;
        }
        #endregion

        #region "Contains"
        /// <summary>
        /// TODO: Optimize this
        /// </summary>
        /// <typeparam name="TT"></typeparam>
        /// <returns></returns>
        public override IEnumerator<TT> GetEnumerator<TT>()
        {
            var x = Records.Find(FilterDefinition<TRecord>.Empty);
            return x.ToEnumerable().Cast<TT>().GetEnumerator();
            //return AsQueryable
        }

        public override IEnumerable<TT> Enumerable<TT>()
        {
            var x = Records.Find(FilterDefinition<TRecord>.Empty).ToEnumerable().Cast<TT>();
            return x;
        }

        public override IEnumerable<TRecord> Where(Expression<Func<TRecord, bool>> predicate, int count = 0, int skip = 0)
        {
            //FilterDefinition<TRecord> filterDefinition = Builders<TRecord>.Filter.Where(predicate); 
            var qrx = Builders<TRecord>.Filter.Where(predicate);
            var cursor = Records.Find(qrx);
            if (count > 0) cursor.Limit(count);
            if (skip > 0) cursor.Skip(skip);
            return cursor.ToEnumerable();
        }



        public override IEnumerable<TRecord> Where(DataQuery predicate)
        {
            throw new NotImplementedException();
            //            var query = Query<TRecord>.Where(predicate.Compile());
            //            if(predicate.HasIn)
            //            MongoCursor<TRecord> cursor = Records.Find(query);
            //            return cursor;
        }

        public override IEnumerable<TRecord> In<TMember>(Expression<Func<TRecord, TMember>> func, IEnumerable<TMember> values)
        {
            var f = Builders<TRecord>.Filter.In(func, values);
            var cursor = Records.Find(f);
            return cursor.ToCursor().ToEnumerable();
        }
        public override List<Index> GetIndexes()
        {
            if (_indexes != null) return _indexes;
            var rawIndexes = Records.Indexes.ListAsync().Result.ToEnumerable();
            var indexes = rawIndexes.Select(x =>
                new Index(x["name"].ToString(),
                        ((BsonDocument)x["key"])
                        .Where(y => !y.Name.StartsWith("_fts"))
                        .Select(y => new IndexKey(y.Name, byte.Parse(y.Value.ToString()))).ToList(),
                        x.Contains("unique") && x["unique"] == true)
                )
                .ToList();
            _indexes = indexes;
            return indexes;
        }

        public override bool Any(Expression<Func<TRecord, bool>> predicate)
        {
            var qrx = Builders<TRecord>.Filter.Where(predicate);
            TRecord data = Records.Find<TRecord>(qrx).First();
            return data != null;
        }

        public override TRecord FirstOrDefault(Expression<Func<TRecord, bool>> predicate)
        {
            var one = First(predicate);
            return one == null ? default(TRecord) : one;
        }

        public override TRecord First(Expression<Func<TRecord, bool>> predicate)
        {
            //FilterDefinition<TRecord> filterDefinition = Builders<TRecord>.Filter.Where(predicate); 
            var qrx = Builders<TRecord>.Filter.Where(predicate);
            return Records.Find(qrx).First();
        }

        /// <summary>
        /// Gets if an element exists by checking for it's id.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public override bool Exists(TRecord obj, ref TRecord doc)
        {
            doc = FindFirst(obj);
            return doc != null;
        }

        public bool Exists(TRecord obj, ref TRecord doc, ref FilterDefinition<TRecord> qrOut)
        {
            doc = FindFirst(obj, qrOut);
            return doc != null;
        }


        /// <summary>
        /// Gets if an element exists by checking for it's id.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public override bool Exists(object obj, ref object doc)
        {
            doc = Find((TRecord)obj).FirstOrDefault();
            return doc != null;
        }

//        public override bool Exists(Entity element, ref TypedEntity value)
//        {
//            throw new NotImplementedException();
//        }

        /// <summary>   Determine if a document exists, using a predicate. </summary>
        ///
        /// <remarks>   Vasko, 03-Dec-17. </remarks>
        ///
        /// <param name="key">Predicate to check with</param>
        /// <param name="doc">  The document. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>

        public override bool Exists(Expression<Func<TRecord, bool>> key, TRecord doc)
        {
            doc = FindOne(key);
            return doc != null;
        }
         

        /// <summary>
        /// Gets if an id exists.
        /// </summary>
        /// <param name="id">The id we're searching for</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool Exists(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            var qrx = Builders<TRecord>.Filter.Eq("_id", id);
            var res = Records.Find(qrx)
                .Limit(1)
                .FirstOrDefault();
            return res != null;
        }

        public bool Exists(Predicate<TRecord> predicate)
        {
            return (from x in Records.AsQueryable() where predicate(x) select x).FirstOrDefault() != null;
        }
        /**
        **/

        public bool Contains(params KeyValuePair<string, object>[] matchingkeys)
        {
            TRecord wrc = Records.Find(CreateQuery(matchingkeys)).First();
            return wrc != null;
        }
        public bool Contains(params object[] keys)
        {
            TRecord wrc = Records.Find(CreateQuery(keys)).First();
            return wrc != null;
        }

        #endregion

        #region "Item access"

        public TRecord this[string id]
        {
            get { return Records.Find(Builders<TRecord>.Filter.Eq("_id", id)).ToList().FirstOrDefault(); }
            set { Records.SaveOrReplace(value); }
        }
        public TRecord this[Int32 offset, Dictionary<string, string> qr = null]
        {
            get
            { 
                var cursor = Records.Find<TRecord>(FilterDefinition<TRecord>.Empty);
                cursor.Skip(offset > 0 ? offset - 1 : 0);
                cursor.Limit(1);
                return cursor.ToList().FirstOrDefault();
            }
            set
            {
                throw new Exception("Not implemented!");
//                if (offline) base[offset] = value;
//                var cursor = Records.Find<TRecord>(FilterDefinition<TRecord>.Empty);
//                cursor.Skip(offset - 1);
//                cursor.Limit(1);
            }
        }

        public IEnumerable<TRecord> Items(Func<TRecord, bool> predicate)
        {
            return this.QueryableCollection1.Where(predicate);
        }
        public IAsyncCursor<TRecord> Items(FilterDefinition<TRecord> qr)
        {
            return this.Records.Find(qr).ToCursor();
        }

        public List<TRecord> Items(int offset, int count)
        {
            var cursor = Records.Find(FilterDefinition<TRecord>.Empty);
            cursor.Skip(offset > 0 ? offset - 1 : 0);
            cursor.Limit(count);
            List<TRecord> @out = new List<TRecord>();
            int i = 0;
            var cnt = cursor.Count();
            foreach (var element in cursor.ToEnumerable())
            {
                if (i > cnt) break;
                @out.Add(element);
                i++;
            }
            return @out;
        }

        #endregion


        #region "Find functions"
        /// <summary>
        /// Find all elements of a collection
        /// </summary>
        /// <returns>returns all the elements of a collection</returns>
        public IList<TRecord> FindAll()
        {
            var result = Records.Find(FilterDefinition<TRecord>.Empty).ToEnumerable();
            return result.ToList();
        }
        public IEnumerable<TRecord> Find(TRecord obj)
        {
            if (obj==null) return null;
            var query = DbQueryProvider.GetInstance().GetUniqueQuery(this, obj);
            //if (query == null) query = Builders<TRecord>.Filter.Eq(x => x.Id, obj.Id);
            var res = Records.Find(query).ToEnumerable();
            return res;
        }

        public TRecord FindFirst(TRecord elementToMatch)
        {
            FilterDefinition<TRecord> qrx = null;
            return FindFirst(elementToMatch, qrx);
        }

        /// <summary>   Searches for the first first. </summary>
        ///
        /// <remarks>   Vasko, 03-Dec-17. </remarks>
        ///
        /// <param name="elementToMatch"> The matching. </param>
        /// <param name="qrOut">    The matching query</param>
        /// <returns>   The found first. </returns>

        public TRecord FindFirst(TRecord elementToMatch, FilterDefinition<TRecord> qrOut)
        {
            qrOut = DbQueryProvider.GetInstance().GetUniqueQuery(this, elementToMatch);
            return Records.Find(qrOut).First();
        }

        /// <summary>   Searches for the first first. </summary>
        ///
        /// <remarks>   Vasko 03-Dec-17. </remarks>
        ///
        /// <exception cref="IpGeoDbConnectionException">   Thrown when an IP Geo Database Connection
        ///                                                 error condition occurs. </exception>
        ///
        /// <returns>   The found first. </returns>

        public TRecord FindFirst()
        {
            if (!Connected)
                throw new IpGeoDbConnectionException("Cannot connect to mongodb server " + URL + "!");
            TRecord obj = Records.Find(FilterDefinition<TRecord>.Empty).First();
            return (TRecord)typeof(TRecord).Ctor(typeof(TRecord), obj);
            // obj("")
        }

        public IEnumerable<TRecord> Find(GeoLoc coords)
        {
            if (!Connected)
                return null; 
            var qr = Builders<TRecord>.Filter.Near(GeospaceIndex, coords.LAT, coords.LNG);
            return (from obj in Records.Find(qr).ToEnumerable()
                    select typeof(TRecord).Ctor(typeof(TRecord), obj)).Cast<TRecord>();
        }
        public IEnumerable<TRecord> Find(Expression<Func<TRecord, bool>> key)
        {
            IEnumerable<TRecord> res = QueryableCollection1.Where(key);
            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TRecord FindOne(Expression<Func<TRecord, bool>> key)
        {
            return QueryableCollection1.FirstOrDefault(key);
        }
        public TRecord FindOne(GeoLoc coords)
        {
            if (!Connected)
                return null;
            //"geolocation":{"$near":[38.022626,-84.499855]}
            var qr = Builders<TRecord>.Filter.Near(GeospaceIndex, coords.LAT, coords.LNG);
            var entry = Records.Find(qr).First();
            return typeof(TRecord).Ctor(typeof(TRecord), entry) as TRecord;
        }

        /// <summary>   Updates the first record matching the location.</summary>
        ///
        /// <remarks>   Vasko 03-Dec-17. </remarks>
        ///
        /// <param name="coords">   The coordinates. </param>
        /// <param name="update">   The update. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>

        public bool UpdateOne(GeoLoc coords, TRecord update)
        {
            if (!Connected) return false;
            //"geolocation":{"$near":[38.022626,-84.499855]}
            var qr = Builders<TRecord>.Filter.Near(GeospaceIndex, coords.LAT, coords.LNG);
            var res = Records.ReplaceOne(qr, update, new UpdateOptions()
            {
                IsUpsert = true
            });
            return res.IsAcknowledged;
        }

         
        #endregion

        #region "Saving"

        public override bool Save(IEnumerable elements)
        {
            foreach (TRecord element in elements)
            {
                this.Save(element);
            }
            return false;
        }

        public override bool SaveOrUpdate(TRecord newElem)
        {
            return SaveOrUpdate<Object>(newElem, null, null);
        }

        public bool UpdateAll(IEnumerable<TRecord> elements, int batchSize= 3000)
        {
            var updateBatcher = new MongoUpdateBatch<TRecord>(Records, 3000);
            return false;
        }

        /// <summary>
        /// Saves or updates an existing document.
        /// Selection is done by a member selector expression or by index filtering.
        /// </summary>
        /// <typeparam name="TMember"></typeparam>
        /// <param name="newElem">The new state of the document</param>
        /// <param name="memberSelector">The member to test, null if you'll be using index queries</param>
        /// <param name="value">The value to test against</param>
        /// <returns></returns>
        public override bool SaveOrUpdate<TMember>(TRecord newElem, Expression<Func<TRecord, TMember>> memberSelector, TMember value)
        {
            FilterDefinition<TRecord> matchingQuery = null;
            //newElem.PrepareForSaving();
            TRecord existingElement = null;
            if (memberSelector != null)
            {
                matchingQuery = Builders<TRecord>.Filter.Eq(memberSelector, value);
            }
            else
            {
                matchingQuery = DbQueryProvider.GetInstance().GetUniqueQuery(this, newElem);
            }
            existingElement = ((matchingQuery).ToBsonDocument().ElementCount == 0) ?
                null : Records.Find(matchingQuery).First();
            //If the document does not exist, save it
            if (existingElement == null)
            {
                Save(newElem);
                return true;
            }
            else
            {
                var keymember = DbQueryProvider.GetInstance().GetKeyMember<TRecord>();
                var existingKey = keymember.GetValue(existingElement);
                //Else just use the id to update the document
                keymember.SetValue(newElem, existingKey); 
                //BsonDocument updatedDoc = existingElement.ToBsonDocument().Merge(newElem.ToBsonDocument(), true);
                var update = Records.ReplaceOne(matchingQuery, newElem);
                return update.IsAcknowledged;
            }

        }

        public override bool SaveOrUpdate(Expression<Func<TRecord, bool>> predicate, TRecord replaceWith)
        {
            var query = Builders<TRecord>.Filter.Where(predicate);
            //IMongoUpdate mongoUpdate = Update.Replace(replaceWith);
            var res = Records.ReplaceOne(query, replaceWith, new UpdateOptions()
            {
                IsUpsert = true
            });
            return res.IsAcknowledged;
        }
        public override bool SaveOrUpdate(IEnumerable<TRecord> elements)
        {
            foreach (var element in elements)
            {
                SaveOrUpdate(element);
            }
            return true;
        }

        public override IEnumerable<TRecord> Range(int skip, int limit)
        {
            var result = Records.Find(FilterDefinition<TRecord>.Empty);
            result.Skip(skip);
            result.Limit(limit as int?);
            return result.ToList();
        }

        public override bool DeleteAll(IEnumerable<TRecord> elements, CancellationToken? cancellationToken = null)
        {
            return DeleteAllAsync(elements, cancellationToken).Result;
        }
        public override async Task<bool> DeleteAllAsync(IEnumerable<TRecord> elements, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;

            var queryList = new List<FilterDefinition<TRecord>>();
            foreach (var element in elements)
            {
                var query = DbQueryProvider.GetInstance().GetUniqueQuery(this, element);
                queryList.Add(query);
            }
            var orredQuery = Builders<TRecord>.Filter.Or(queryList);
            var result = await Records.DeleteManyAsync(orredQuery, cancellationToken.Value);
            return result.IsAcknowledged;
        }

        public override bool Delete(TRecord elem)
        { 
            var qrOut = DbQueryProvider.GetInstance().GetUniqueQuery(this, elem);
            var delResult = Records.DeleteOne(qrOut);
            return delResult.IsAcknowledged;
        } 

        //public override object Database { get; set; }

        public override void Save(TRecord elem)
        {
            var recordType = typeof(TRecord); 
            //elem.PrepareForSaving();
            try
            {
                var writeConcernResult = Records.SaveOrReplace<TRecord>(elem);
                //var tempx = writeConcernResult.ToJson(); 
                if (!writeConcernResult.IsAcknowledged)
                {
                    throw new Exception(writeConcernResult.ToString());
                } 
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
 
        public override void AddRange(IEnumerable<TRecord> elems)
        {
            //elem.PrepareForSaving();
            try
            {
                var options = new InsertManyOptions() { };
                Records.InsertMany(elems, options);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        public bool TrySave(TRecord data)
        {
            try
            {
                Save(data);
                return true;
            }
            catch  
            {
                return false;
            }
        }

        /// <summary>   Saves the given elements. </summary>
        ///
        /// <remarks>   Vasko, 03-Dec-17. </remarks>
        ///
        /// <param name="elems">    The Elements to save. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>

        public override bool Save(IEnumerable<object> elems)
        {
            foreach (var el in elems)
            {
                //el.PrepareForSaving();
                Records.SaveOrReplace(el as TRecord);
            }
            return true;
        }
        #endregion

        #region "Query building"

        /// <summary>   Creates a query. </summary>
        ///
        /// <remarks>   Vasko, 03-Dec-17. </remarks>
        ///
        /// <param name="keys"> A variable-length parameters list containing keys. </param>
        ///
        /// <returns>   The new query. </returns>

        public FilterDefinition<TRecord> CreateQuery(params object[] keys)
        {
            FilterDefinition<TRecord> qr = null;
            List<FilterDefinition<TRecord>> qrtmp = new List<FilterDefinition<TRecord>>();
            for (Int32 iqk = 0; iqk <= keys.Length - 1; iqk++)
            {
                string key = keys[iqk].ToString();
                BsonValue vl = BsonValue.Create(keys[iqk]);
                qrtmp.Add(Builders<TRecord>.Filter.Eq(key, vl));
            }
            qr = Builders<TRecord>.Filter.And(qrtmp.ToArray());
            return qr;
        }

        /// <summary>   Converts the matchingkeys to a query. </summary>
        ///
        /// <remarks>   Vasko 03-Dec-17. </remarks>
        ///
        /// <param name="matchingkeys"> A variable-length parameters list containing matchingkeys. </param>
        ///
        /// <returns>   Matchingkeys as a FilterDefinition&lt;TRecord&gt; </returns>
        public FilterDefinition<TRecord> CreateQuery(params KeyValuePair<string, object>[] matchingkeys)
        {
            FilterDefinition<TRecord> qr = null;
            List<FilterDefinition<TRecord>> qrtmp = new List<FilterDefinition<TRecord>>();
            for (Int32 iqk = 0; iqk <= matchingkeys.Length - 1; iqk++)
            {
                string key = matchingkeys[iqk].Key;
                BsonValue vl = BsonValue.Create((object)matchingkeys[iqk].Value);
                var newQuery = Builders<TRecord>.Filter.Eq(key, vl);
                qrtmp.Add(newQuery);
            }
            qr = Builders<TRecord>.Filter.And(qrtmp.ToArray());
            return qr;
        }
        #endregion

        /// <summary>
        /// Tries to transfer all documents from the target, to the current database.
        /// </summary>
        /// <param name="targetDb"></param>
        /// <param name="targetCollection"></param>
        /// <remarks></remarks>
        public void SyncFrom(string targetDb, string targetCollection)
        {
            MongoList<TRecord> target = new MongoList<TRecord>(targetDb, targetCollection);
            foreach (var doc in target.FindAll())
            {
                try
                {
                    this.Records.SaveOrReplace(doc);
                }
                catch (Exception ex)
                { 
                    Trace.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Drops and recreates a collection
        /// </summary>
        public override bool Trash()
        {
            var name = Records.CollectionNamespace.CollectionName;
            Database.DropCollection(name);
            //Database.CreateCollection(name);
            return true;
        }

        public async Task<bool> CollectionExistsAsync(IMongoDatabase database, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            //filter by collection name
            var collections = await database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            //check for existence
            return await collections.AnyAsync();
        }
         

        public void EnsureIndex(string indexKey)
        {
            var index = Builders<TRecord>.IndexKeys.Ascending(indexKey);
            var indexName = indexKey + "_1";
            if (!Records.IndexExists(indexName))
            {
                Records.Indexes.CreateOne(index, new CreateIndexOptions
                {
                    Name = indexName
                });
            }

        }

        public string GetCollectionName()
        {
            return Records?.CollectionNamespace?.CollectionName;
        }
    }
}