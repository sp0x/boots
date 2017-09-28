using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public class MongoSource : InputSource
    {
        private IMongoCollection<BsonDocument> _collection;
        private BsonDocument _cachedInstance;
        private object _lock;
        private IAsyncCursor<BsonDocument> _cursor;
        private FilterDefinition<BsonDocument> _query;
        private IFindFluent<BsonDocument, BsonDocument> _finder;
        private Func<BsonDocument, BsonDocument> _project;

        public MongoSource(string collectionName, IInputFormatter formatter) : base(formatter)
        {
            var list = new MongoList(DBConfig.GetGeneralDatabase(), collectionName);
            _collection = list.Records;
            _lock = new object();
            _query = Builders<BsonDocument>.Filter.Empty;
        }

        public MongoSource SetProjection(Func<BsonDocument, BsonDocument> project)
        {
            _project = project;
            return this;
        }

        public override IIntegrationTypeDefinition GetTypeDefinition()
        {
            var firstElement = _collection.Find(Builders<BsonDocument>.Filter.Empty).First();
            try
            {
                var firstInstance = _cachedInstance = firstElement;
                IntegrationTypeDefinition typedef = null;
                if (firstInstance != null)
                {
                    if (_project != null) firstInstance = _project(firstInstance);
                    typedef = new IntegrationTypeDefinition(_collection.CollectionNamespace.CollectionName);
                    typedef.CodePage = Encoding.CodePage;
                    typedef.OriginType = Formatter.Name;
                    typedef.ResolveFields(firstInstance);
                }
                return typedef;
            }
            catch (Exception ex)
            {

                Debug.WriteLine(ex.Message);
                Trace.WriteLine(ex.Message);
            }
            return null;
        }

        public override dynamic GetNext()
        {
            lock (_lock)
            {
                dynamic lastInstance = null;
                var resetNeeded = _cachedInstance != null;
                if (resetNeeded || _finder == null)
                {
                    _finder = _collection.Find(_query);
                }
                if (resetNeeded || _cursor==null)
                {
                    _cursor = _finder.ToCursor();
                    _cachedInstance = null;
                }
                var item = (Formatter as BsonFormatter).GetNext(_finder, resetNeeded);
                if (item == null) return item;
                if (_project != null && item!=null) item = _project(item);
                else if (item == null){
                    item = item;
                }
                lastInstance = BsonSerializer.Deserialize<ExpandoObject>(item);
                return lastInstance;
            }
        }

        public override void Cleanup()
        {
            if (Formatter != null)
            {
                Formatter.Dispose();
                _cursor.Dispose();
            }
        }

        public override IEnumerable<InputSource> Shards()
        {
            yield return this;
        }

        public override void DoDispose()
        {
            _cursor.Dispose();
            _collection = null;
        }

        public override string ToString()
        {
            return _collection == null ? base.ToString() : _collection.CollectionNamespace.FullName;
        }

        public static MongoSource CreateFromCollection(string collectionName, IInputFormatter formatter)
        {
            var source = new MongoSource(collectionName, formatter);
            return source;
        }

    }
}