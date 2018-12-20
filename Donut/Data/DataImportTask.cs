using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Donut.Encoding;
using Donut.Integration;
using Donut.Lex;
using Donut.Lex.Expressions;
using Donut.Lex.Parsing;
using Donut.Parsing.Tokenizers;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces.Blocks;
using Netlyt.Interfaces.Data;

namespace Donut.Data
{
    /// <summary>   A data import task which reads the input source, registers an api data type for it and fills the data in a new collection. </summary>
    ///
    /// <remarks>   Vasko, 14-Dec-17. </remarks>
    ///
    /// <typeparam name="T">    Generic type parameter for that data that will be read. Use ExpandoObject if not sure. </typeparam>

    public class DataImportTask<T> where T : class
    {
        private DataImportTaskOptions _options;
        private Harvester<T> _harvester;
        private IIntegration _integration;
        public IIntegration Integration
        {
            get
            {
                return _integration;
            }
            private set
            {
                _integration = value;
            }
        }
        public DestinationCollection OutputDestinationCollection { get; private set; }
        private FieldEncoder _encoder;
        public bool EncodeOnImport { get; set; } = false;
        public DataImportTaskOptions Options => _options;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public DataImportTask(DataImportTaskOptions options)
        {
            _options = options;
            string tmpGuid = Guid.NewGuid().ToString();
            _harvester = new Harvester<T>(_options.ThreadCount);
            if (options.Integration != null)
            {
                if (options.Integration.Fields.Count == 0)
                {
                    throw new InvalidOperationException("Integration needs to have at least 1 field.");
                }
                _integration = options.Integration;
                _integration.APIKey = _options.ApiKey;
                if (string.IsNullOrEmpty(_integration.FeaturesCollection))
                {
                    _integration.FeaturesCollection = $"{_integration.Collection}_features";
                }
                if (string.IsNullOrEmpty(_integration.Collection))
                {
                    _integration.Collection = tmpGuid;
                }
                _harvester.AddIntegration(options.Integration, _options.Source);
            }
            else
            {
                _integration = _harvester.AddIntegrationSource(_options.Source, _options.ApiKey,
                    _options.IntegrationName, tmpGuid);
            }

            var outCollection = new DestinationCollection(_integration.Collection, _integration.GetReducedCollectionName());
            OutputDestinationCollection = outCollection;
            if (options.TotalEntryLimit > 0) _harvester.LimitEntries(options.TotalEntryLimit);
            if (options.ShardLimit > 0) _harvester.LimitShards(options.ShardLimit);
            this.EncodeOnImport = options.EncodeInput;
            if (this.EncodeOnImport)
            {
                _encoder = FieldEncoder.Factory.Create(_integration);
            }
            // new OneHotEncoding(new FieldEncodingOptions { Integration = _integration });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataImportResult> Import(CancellationToken? cancellationToken = null, bool truncateDestination = false)
        {
            if (Options.TotalEntryLimit > 0) _harvester.LimitEntries(Options.TotalEntryLimit);
            if (Options.ShardLimit > 0) _harvester.LimitShards(Options.ShardLimit);

            var database = MongoHelper.GetDatabase();
            if (truncateDestination)
            {
                database.Truncate(OutputDestinationCollection.OutputCollection);
                //Debug.WriteLine($"Created temp collections: {dstCollection.GetCollectionName()} & {OutputDestinationCollection.ReducedOutputCollection}");
            }

            var dstCollection = database.GetCollection<BsonDocument>(OutputDestinationCollection.OutputCollection);
            var batchesInserted = 0;
            var batchSize = _options.ReadBlockSize;
            var executionOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, };

            var toBsonDocBlock = new TransformBlock<ExpandoObject, BsonDocument>((o) =>
            {
                var doc = o.ToBsonDocument();
                //Apply encoding here
                if(EncodeOnImport) EncodeImportDocument(doc);
                return doc;
            });//BsonConverter.CreateBlock(new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
            var readBatcher = BatchedBlockingBlock<BsonDocument>.CreateBlock(batchSize);
            //readBatcher.LinkTo(toBsonDocBlock, new DataflowLinkOptions { PropagateCompletion = true });
            var inserterBlock = new ActionBlock<IEnumerable<BsonDocument>>(x =>
            {
                Debug.WriteLine($"Inserting batch {batchesInserted + 1} [{x.Count()}]");
                dstCollection.InsertMany(x);
                Interlocked.Increment(ref batchesInserted);
                Debug.WriteLine($"Inserted batch {batchesInserted}");
            }, executionOptions);
            toBsonDocBlock.LinkTo(readBatcher, new DataflowLinkOptions { PropagateCompletion = true });
            readBatcher.LinkTo(inserterBlock, new DataflowLinkOptions { PropagateCompletion = true });

            var result = await _harvester.ReadAll(toBsonDocBlock, cancellationToken);
            await Task.WhenAll(inserterBlock.Completion, toBsonDocBlock.Completion);
            foreach (var index in _options.IndexesToCreate)
            {
                dstCollection.EnsureIndex(index);
            }
            var output = new DataImportResult(result, dstCollection, _integration);
            return output;
        }

        /// <summary>
        /// Applies encoding to a single imported documment.
        /// </summary>
        /// <param name="doc"></param>
        private void EncodeImportDocument(BsonDocument doc)
        {
            _encoder.Apply(doc);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="donutScript">The reduce script to execute.</param>
        /// <param name="inputDocumentsLimit"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public async Task Reduce(string donutScript, uint inputDocumentsLimit = 0, SortDefinition<BsonDocument> orderBy = null)
        {
            MapReduceExpression mapReduce = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions())
                .Tokenize(donutScript)).ReadMapReduce();
            MapReduceJsScript script = MapReduceJsScript.Create(mapReduce);
            var targetCollection = OutputDestinationCollection.ReducedOutputCollection;
            var mapReduceOptions = new MapReduceOptions<BsonDocument, BsonDocument>
            {
                Sort = orderBy,
                JavaScriptMode = true,
                OutputOptions = MapReduceOutputOptions.Replace(targetCollection)
            };
            if (inputDocumentsLimit > 0) mapReduceOptions.Limit = inputDocumentsLimit;
            var collection = MongoHelper.GetCollection(OutputDestinationCollection.OutputCollection);
            await collection.MapReduceAsync<BsonDocument>(script.Map, script.Reduce, mapReduceOptions);
        }
        /// <summary>
        /// Encodes a collection
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task Encode(IMongoCollection<BsonDocument> collection, CancellationToken? ct = null)
        {
            if (ct == null) ct = CancellationToken.None;
            await _encoder.ApplyToAllFields(collection, ct);
        }
    }
}
