using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Netlyt.Interfaces;

namespace Donut
{
    public abstract class DataSet<T> : IDataSet<T>
        where T : class
    {
        protected MongoClient Connection { get; private set; }
        public IMongoCollection<T> Records { get; private set; }
        protected IMongoDatabase Database { get; private set; }
        
        public void SetSource(string collection, MongoUrl url, string dbName)
        {
            //var dbConfig = DBConfig.GetGeneralDatabase();
            Connection = new MongoClient(url);
            Database = Connection.GetDatabase(dbName);
            Records = Database.GetCollection<T>(collection);
        }

        public void SetSource(string collection)
        {
            throw new NotImplementedException();
        }

        public void SetAggregateKeys(IEnumerable<IAggregateKey> keys)
        {
            throw new NotImplementedException();
        }

        public IMongoQueryable<T> AsQueryable()
        {
            return Records.AsQueryable();
        }
    }
}
