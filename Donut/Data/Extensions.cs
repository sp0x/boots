using System;
using System.Diagnostics;
using Donut.Integration;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces.Data;

namespace Donut.Data
{
    public static class Extensions
    {
        public static IMongoCollection<T> GetMongoCollection<T>(this IIntegration ign)
        {
            var tcol = ign.Collection;
            var mcol = MongoHelper.GetCollection<T>(tcol);
            return mcol;
        }
        public static IMongoCollection<T> GetMongoFeaturesCollection<T>(this IIntegration ign)
        {
            var tcol = ign.FeaturesCollection;
            var mcol = MongoHelper.GetCollection<T>(tcol);
            return mcol;
        }
        /// <summary>
        /// Drops and recreates a collection
        /// </summary>
        public static void Truncate(this IMongoDatabase db, string nsps)
        {
            db.DropCollection(nsps);
            try
            {
                db.CreateCollection(nsps);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Could not create collection after drop!");
            }
        }
        /// <summary>
        /// Drops and recreates a collection
        /// </summary>
        public static void Truncate<T>(this IMongoCollection<T> coll)
        {
            coll.Database.DropCollection(coll.CollectionNamespace.CollectionName);
            try
            {
                coll.Database.CreateCollection(coll.CollectionNamespace.CollectionName);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Could not create collection after drop!");
            }
        }
        public static void EnsureIndex(this IMongoCollection<BsonDocument> collection, string indexKey)
        {
            var index = Builders<BsonDocument>.IndexKeys.Ascending(indexKey);
            var indexName = indexKey + "_1";
            if (!collection.IndexExists(indexName))
            {
                collection.Indexes.CreateOne(index, new CreateIndexOptions
                {
                    Name = indexName
                });
            }
        }
        public static bool IndexExists<TRecord>(this IMongoCollection<TRecord> collection, string indexName)
        {
            var indexes = collection.Indexes.List().ToList();
            foreach (var index in indexes)
            {
                if (index["name"] == indexName)
                {
                    return true;
                }
            }
            return false;
        }
        public static void Drop(this IMongoCollection<BsonDocument> collection)
        {
            collection.Database.DropCollection(collection.CollectionNamespace.CollectionName);
        }
    }
}