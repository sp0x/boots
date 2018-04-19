using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.Batching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.exec.Blocks;
using Netlyt.Service.Integration.Encoding;
using Netlyt.Service.Lex;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Parsing;
using Netlyt.Service.Lex.Parsing.Tokenizers;

namespace Netlyt.Service.Integration.Import
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
        private DataIntegration _integration;
        public DataIntegration Integration
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
        private FieldEncoder _oneHotEncoding;
        public bool EncodeOnImport { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiService"></param>
        /// <param name="integrationService"></param>
        /// <param name="options"></param>
        public DataImportTask(ApiService apiService, IntegrationService integrationService, DataImportTaskOptions options)
        {
            _options = options;
            string tmpGuid = Guid.NewGuid().ToString();
            _harvester = new Harvester<T>(apiService, integrationService, _options.ThreadCount);
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
                _harvester.AddType(options.Integration, _options.Source);
            }
            else
            {
                _integration = _harvester.AddIntegrationSource(_options.Source, _options.ApiKey,
                    _options.IntegrationName, true, tmpGuid);
            }

            var outCollection = new DestinationCollection(_integration.Collection, _integration.GetReducedCollectionName());
            OutputDestinationCollection = outCollection;
            if (options.TotalEntryLimit > 0) _harvester.LimitEntries(options.TotalEntryLimit);
            if (options.ShardLimit > 0) _harvester.LimitShards(options.ShardLimit);
            _oneHotEncoding = FieldEncoder.Factory.Create(_integration);// new OneHotEncoding(new FieldEncodingOptions { Integration = _integration });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataImportResult> Import(CancellationToken? cancellationToken = null, bool truncateDestination = false)
        {

            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var dstCollection = new MongoList(databaseConfiguration, OutputDestinationCollection.OutputCollection);

            if (truncateDestination)
            {
                dstCollection.Truncate();
                Debug.WriteLine($"Created temp collections: {dstCollection.GetCollectionName()} & {OutputDestinationCollection.ReducedOutputCollection}");
            }

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
                dstCollection.Records.InsertMany(x);
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

        private void EncodeImportDocument(BsonDocument doc)
        {
            _oneHotEncoding.Apply(doc);
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
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var sourceCollection = new MongoList(databaseConfiguration, OutputDestinationCollection.OutputCollection);
            await sourceCollection.Records.MapReduceAsync<BsonDocument>(script.Map, script.Reduce, mapReduceOptions);
        }

        public async Task Encode(CancellationToken? ct = null)
        {
            if (ct == null) ct = CancellationToken.None;
            await _oneHotEncoding.ApplyToAllFields(ct);
        }
    }
}
