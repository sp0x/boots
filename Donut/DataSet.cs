using System;
using System.Collections.Generic;
using Donut.Data;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;

namespace Donut
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
            Database = MongoHelper.GetDatabase();;
            var cinst = Database.GetCollection<T>(collection);
            Records = cinst;
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
