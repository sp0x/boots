using System;
using System.Collections.Generic;
using System.Text;
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
            murlBuilder.AuthenticationSource = "admin";
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
