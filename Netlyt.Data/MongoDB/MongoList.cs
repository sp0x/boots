using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Netlyt.Data.MongoDB
{
    public interface IMongoList
    {
        IMongoDatabase Database { get; }
    }
    
    public class MongoList
        : IDbListBase, IMongoList
    {
        private string DbName { get; set; }
        public MongoClient Connection { get;  private set; }
        public IMongoCollection<BsonDocument> Records { get; private set; }
        public IMongoDatabase Database { get; private set; } 
        
//        public MongoList(DatabaseConfiguration dbc, string collection)
//        {
//            Connection = new MongoClient(dbc.Value);
//            Database = Connection.GetDatabase(dbc.GetDatabaseName());
//            if (collection == null) throw new ArgumentNullException(nameof(collection));
//            if (null == (Records = Database.GetCollection<BsonDocument>(collection)))
//                Database.CreateCollection(collection);
//            Records = Database.GetCollection<BsonDocument>(collection); 
//        }

        /// <summary>
        /// Drops and recreates a collection
        /// </summary>
        public void Truncate()
        {
            var name = Records.CollectionNamespace.CollectionName;
            Database.DropCollection(name);
            try
            {
                Database.CreateCollection(name);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Could not create collection after drop!");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbnm">The name of the database to use</param>
        /// <param name="collection">The name of the collection to use</param>
        /// <param name="url">A connection url for mongodb</param> 
        public MongoList(string dbnm, string collection, string url = null)
        {
            if (string.IsNullOrEmpty(url)) url = DBConfig.DefaultMongoHost;
            DbName = dbnm;
            var murlBuilder = new MongoUrlBuilder(url);
            murlBuilder.AuthenticationSource = "admin";
            var murl = murlBuilder.ToMongoUrl();
            Connection = new MongoClient(murl);
            Database = Connection.GetDatabase(DbName); 
            if (null == (Records = Database.GetCollection<BsonDocument>(collection)))
                Database.CreateCollection(collection);
            Records = Database.GetCollection<BsonDocument>(collection); 
        }

        public static bool CollectionExists(string collection, string connectionUrl = null)
        {
            if (string.IsNullOrEmpty(connectionUrl)) connectionUrl = DBConfig.DefaultMongoHost; 
            var murlBuilder = new MongoUrlBuilder(connectionUrl);
            murlBuilder.AuthenticationSource = "admin";
            var murl = murlBuilder.ToMongoUrl();
            var client = new MongoClient(murl); 
            var database = client.GetDatabase(murlBuilder.DatabaseName);
            var collObject = database.GetCollection<BsonDocument>(collection);
            return collObject!=null;
        }

        public void EnsureIndex(string indexKey)
        {
            var index = Builders<BsonDocument>.IndexKeys.Ascending(indexKey);
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

        public IEnumerator GetEnumerator()
        {
            return Records.AsQueryable().GetEnumerator();
        }

        public bool Connected
        {
            get { return Connection != null; }
        }

        public string CollectionName
        {
            get { return GetCollectionName(); }
        }

        public int Size {
            get
            {
                var filterDefinition = new BsonDocument();
                return (int)Records.Find(filterDefinition).Count();
            }
        }

        public IQueryable AsQueryable
        {
            get { return Records.AsQueryable(); }
        }
         
//
//        public IEnumerable<TRecord> Get<TRecord>(Expression<Func<TRecord, bool>> predicate)
//        {
//            throw new NotImplementedException();
//        }

        public IEnumerator<T> GetEnumerator<T>() where T : class
        {
            return Records.AsQueryable().Cast<T>().GetEnumerator();
        }

        public bool Exists(object element, ref object value)
        {
            throw new NotImplementedException();
        }

//        public bool Exists(Entity element, ref TypedEntity value)
//        {
//            throw new NotImplementedException();
//        }

        public void Save(object element)
        {
            throw new NotImplementedException();
        }
        
        public bool Save(IEnumerable<object> elements)
        {
            throw new NotImplementedException();
        }

        public bool Save(IEnumerable elements)
        {
            throw new NotImplementedException();
        }

        public void Add(object element)
        {
            throw new NotImplementedException();
        }

        public void AddRange(IEnumerable<object> element)
        {
            throw new NotImplementedException();
        }

        public bool SaveOrUpdate(object element)
        {
            throw new NotImplementedException();
        }

        public bool SaveOrUpdate(IEnumerable<object> element)
        {
            throw new NotImplementedException();
        }

        public bool Trash()
        {
            var name = Records.CollectionNamespace.CollectionName;
            Database.DropCollection(name);
            return true;
        }

        
    }
}