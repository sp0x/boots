using MongoDB.Bson.Serialization;

namespace Netlyt.Data.MongoDB
{
    /// <summary>
    /// Class which helps with object mapping.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MongoDbClassMap<T>
    {
        protected MongoDbClassMap()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
                BsonClassMap.RegisterClassMap<T>(Map);
        }
    
        public abstract void Map(BsonClassMap<T> cm);
    }
}