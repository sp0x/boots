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
            throw new NotImplementedException();
        }

        public static IMongoCollection<T> GetTypeCollection<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public static IMongoDatabase GetDatabase()
        {
            throw new NotImplementedException();
        }
    }

    public class IMongo
    {

    }
}
