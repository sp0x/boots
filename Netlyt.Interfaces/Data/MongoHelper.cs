using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Netlyt.Interfaces.Data
{
    public class MongoHelper
    {
        public static IMongoCollection<BsonDocument> GetCollection(string collectionName)
        {
            var config = DonutDbConfig.GetConfig();
            return GetCollection(config, collectionName);
        }
        public static IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            var config = DonutDbConfig.GetConfig();
            return GetCollection<T>(config, collectionName);
        }
        public static IMongoCollection<T> GetCollection<T>(IDatabaseConfiguration dbc, string collectionName)
        {
            var db = GetDatabase(dbc);
            IMongoCollection<T> records;
            if (null == (records = db.GetCollection<T>(collectionName)))
                db.CreateCollection(collectionName);
            records = db.GetCollection<T>(collectionName);
            return records;
        }
        public static IMongoCollection<BsonDocument> GetCollection(IDatabaseConfiguration dbc, string collectionName)
        {
            var db = GetDatabase(dbc);
            IMongoCollection<BsonDocument> records;
            if (null == (records = db.GetCollection<BsonDocument>(collectionName)))
                db.CreateCollection(collectionName);
            records = db.GetCollection<BsonDocument>(collectionName);
            return records;
        }

        public static IMongoCollection<T> GetTypeCollection<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public static IMongoDatabase GetDatabase()
        {
            var dbc = DonutDbConfig.GetConfig();
            return GetDatabase(dbc);
        }
        public static IMongoDatabase GetDatabase(IDatabaseConfiguration dbc)
        {
            var murlBuilder = new MongoUrlBuilder(dbc.GetUrl());
            if (!string.IsNullOrEmpty(murlBuilder.Username) && !string.IsNullOrEmpty(murlBuilder.Password))
            {
                murlBuilder.AuthenticationSource = "admin";
            }
            var murl = murlBuilder.ToMongoUrl();
            var connection = new MongoClient(murl);
            var db = connection.GetDatabase(murl.DatabaseName);
            return db;
        }
    }

    public class IMongo
    {

    }
}
