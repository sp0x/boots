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
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
{
    public class MongoSource : InputSource
    {
        private IMongoCollection<BsonDocument> _collection;
        private BsonDocument _cachedInstance;
        private object _lock; 
        private FilterDefinition<BsonDocument> _query;
        private IAsyncCursorSource<BsonDocument> _cursorSource;
        private Func<BsonDocument, BsonDocument> _project;
        private IAggregateFluent<BsonDocument> _aggregate;
        public double ProgressInterval { get; set; } = 0.5;
        private double _lastProgress;
        public IMongoCollection<BsonDocument> Collection => _collection;

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
            BsonDocument firstElement = null;
            if (_aggregate != null)
            {
                firstElement = _aggregate.First();
            }
            else
            {
                _collection.Find(Builders<BsonDocument>.Filter.Empty).First();
            }
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

        private long GetSize()
        {
            if (_aggregate != null)
            {
                _cursorSource = _aggregate; 
                var counter = new BsonDocument
                {
                    { "$group", new BsonDocument
                    {
                        {"_id", "_id"},
                        {"count", new BsonDocument("$sum", 1)}
                    }}
                };
                var newAggregate = _aggregate.AppendStage<BsonDocument>(counter);
                BsonDocument entry = newAggregate.First();
                
                return entry["count"].ToInt64();
            }
            else
            {
                var finder = _collection.Find(_query);
                return finder.Count();
            }
        }

        public override dynamic GetNext()
        {
            lock (_lock)
            {
                dynamic lastInstance = null;
                var resetNeeded = _cachedInstance != null;
                if (resetNeeded || _cursorSource == null)
                {
                    if (_aggregate != null)
                    {
                        _cursorSource = _aggregate;
                        Size = GetSize();
                    }
                    else
                    {
                        _cursorSource = _collection.Find(_query);
                        Size = ((IFindFluent<BsonDocument, BsonDocument>) _cursorSource).Count();
                    }
                }

                if (resetNeeded)
                { 
                    _cachedInstance = null;
                }
                var item = (Formatter as BsonFormatter).GetNext(_cursorSource, resetNeeded);
                if (item == null) return item;
                if (_project != null && item!=null) item = _project(item);
                else if (item == null){ item = item;
                }
                lastInstance = BsonSerializer.Deserialize<ExpandoObject>(item);
#if DEBUG 
                var crProgress = Progress;
                if ((crProgress - _lastProgress) > ProgressInterval)
                { 
                    Debug.WriteLine($"Bson progress: %{Progress:0.0000} of {Size}");
                    _lastProgress = crProgress;
                }
#endif
                
                return lastInstance;
            }
        }

        public override void Cleanup()
        {
            if (Formatter != null)
            {
                Formatter.Dispose();
                //_cursor.Dispose();
            }
        }

        public override IEnumerable<InputSource> Shards()
        {
            yield return this;
        }

        public override void DoDispose()
        {
            //_cursor.Dispose();
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

        /// <summary>
        /// Filters input from a given type
        /// </summary>
        /// <param name="type"></param>
        public void Filter(IntegrationTypeDefinition type)
        {
            if (_collection.CollectionNamespace.CollectionName != "IntegratedDocument")
            {
                throw new Exception("Type definition filters are supported only on the IntegratedDocument collection");
            }
            var def = Builders<BsonDocument>.Filter.Eq("TypeId", type.Id);
            _query = def;
        }

        public IAggregateFluent<BsonDocument> Aggregate(IAggregateFluent<BsonDocument> aggregate)
        {
            _aggregate = aggregate;
            return aggregate;
//                Group(new BsonDocument { { "_id", "$borough" }, { "count", new BsonDocument("$sum", 1) } });
//            var results = await aggregate.ToListAsync();
        }

        public IAggregateFluent<BsonDocument> CreateAggregate()
        {
            var aggregateArgs = new AggregateOptions { AllowDiskUse = true };
            return _collection.Aggregate< BsonDocument>(aggregateArgs); 
        }
    }
}