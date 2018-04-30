using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using Donut.Integration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Data.Format;

namespace Donut.IntegrationSource
{
    public class MongoSource
    {
        public static MongoSource<T> CreateFromCollection<T>(string collectionName, IInputFormatter<T> formatter)
            where T : class
        {
            var source = new MongoSource<T>(collectionName);
            source.SetFormatter(formatter);
            return source;
        }
    }
    public class MongoSource<T> : Donut.IntegrationSource.InputSource where T : class
    {
        private IMongoCollection<BsonDocument> _collection;
        private T _cachedInstance;
        private object _lock; 
        private FilterDefinition<BsonDocument> _query;
        private IAsyncCursorSource<BsonDocument> _cursorSource;
        private Func<T, T> _project;
        private IAggregateFluent<BsonDocument> _aggregate;
        /// <summary>
        /// The amount of elements in each bson chunk
        /// </summary>
        public uint BatchSize { get; set; } = 1000;
        public double ProgressInterval { get; set; } = 0.5;
        private double _lastProgress;
        public IMongoCollection<BsonDocument> Collection => _collection;

        public MongoSource(string collectionName) : base()
        {
            _collection = MongoHelper.GetCollection(collectionName);//new MongoList(DBConfig.GetGeneralDatabase(), collectionName);
            _lock = new object();
            _query = Builders<BsonDocument>.Filter.Empty;
            Size = GetSize();
        }

        public MongoSource<T> SetProjection(Func<T, T> project)
        {
            _project = project;
            return this;
        }

        public override IIntegration ResolveIntegrationDefinition()
        {
            T firstElement = default(T);
            if (_aggregate != null)
            {
                var bsonDocument = _aggregate.First();
                firstElement = BsonSerializer.Deserialize<T>(bsonDocument);
            }
            else
            {
                var bsonDocument = _collection.Find(Builders<BsonDocument>.Filter.Empty).First();
                firstElement = BsonSerializer.Deserialize<T>(bsonDocument);
            }
            try
            {
                var firstInstance = _cachedInstance = firstElement;
                DataIntegration typedef = null;
                if (firstInstance != null)
                {
                    if (_project != null) firstInstance = _project(firstInstance);
                    typedef = CreateIntegrationFromObj(firstInstance, _collection.CollectionNamespace.CollectionName);
                    typedef.Collection = _collection.CollectionNamespace.CollectionName;
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
        public override IEnumerable<TX> GetIterator<TX>()
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
                        var options = new FindOptions
                        {
                            BatchSize = (int)BatchSize
                        };
                        _cursorSource = _collection.Find(_query, options);
                        Size = ((IFindFluent<BsonDocument, BsonDocument>)_cursorSource).Count();
                    }
                }
                if (resetNeeded)
                {
                    _cachedInstance = default(T);
                }
                
                var bsonFormatter = (Formatter as BsonFormatter<T>);
                if (bsonFormatter == null)
                {
                    throw new Exception("Formatter could not be used with the given type!");
                }
                IEnumerable<T> formatterIterator = bsonFormatter?.GetIterator(_cursorSource, resetNeeded);
                foreach (var formattedItem in formatterIterator)
                {
                    var item = formattedItem; 
                    if (_project != null)
                    {
                        //TODO Fix this..
                        var projection = _project(item as T);
                        item = projection as T;
                    }
                    lastInstance = item;//BsonSerializer.Deserialize<ExpandoObject>(item);
#if DEBUG
                    var crProgress = Progress;
                    if ((crProgress - _lastProgress) > ProgressInterval)
                    {
                        Debug.WriteLine($"Bson progress: %{Progress:0.0000} of {Size}");
                        _lastProgress = crProgress;
                    }
#endif
                    yield return lastInstance;
                }
            }
        }
        public override IEnumerable<dynamic> GetIterator(Type targetType = null)
        {
            return GetIterator<ExpandoObject>();
        }

        public override void Cleanup()
        {
            if (Formatter != null)
            {
                Formatter.Dispose();
                //_cursor.Dispose();
            }
            _lastProgress = 0;
        }

        public override IEnumerable<IInputSource> Shards()
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

        /// <summary>
        /// Filters input from a given type
        /// </summary>
        /// <param name="type"></param>
        public void Filter(IIntegration type)
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

        public override void Reset()
        {
            Cleanup();
            base.Reset();
        }
    }
}