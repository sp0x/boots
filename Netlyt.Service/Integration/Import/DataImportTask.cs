using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using nvoid.db.Batching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.exec.Blocks;

namespace Netlyt.Service.Integration.Import
{
    /// <summary>   A data import task which reads the input source, registers an api data type for it and fills the data in a new collection. </summary>
    ///
    /// <remarks>   Vasko, 14-Dec-17. </remarks>
    ///
    /// <typeparam name="T">    Generic type parameter for that data that will be read. Use ExpandoObject if not sure. </typeparam>

    public class DataImportTask<T>
    {
        private DataImportTaskOptions _options;
        private Harvester<T> _harvester;
        private IIntegration _type;
        public IIntegration Type
        {
            get
            {
                return _type;
            }
            private set
            {
                _type = value;
            }
        }
        public CollectionDetails OutputCollection { get; private set; }
        
        public DataImportTask(DataImportTaskOptions options)
        {
            _options = options;
            string tmpGuid = Guid.NewGuid().ToString();
            var outCollection = new CollectionDetails(tmpGuid, $"{tmpGuid}_reduced");
            OutputCollection = outCollection;
            _harvester = new Harvester<T>(_options.ThreadCount);
            _type = _harvester.AddPersistentType(_options.Source, _options.ApiKey, _options.TypeName, true, outCollection.OutputCollection);
        }

        public async Task<DataImportResult> Import()
        {
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var rawEventsCollection = new MongoList(databaseConfiguration, OutputCollection.OutputCollection);

            rawEventsCollection.Truncate();
            Debug.WriteLine($"Created temp collections: {rawEventsCollection.GetCollectionName()} & {OutputCollection.ReducedOutputCollection}");

            var batchesInserted = 0;
            var batchSize = _options.ReadBlockSize;
            var executionOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, };

            var transformerBlock = BsonConverter.CreateBlock(new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
            var readBatcher = BatchedBlockingBlock<ExpandoObject>.CreateBlock(batchSize);
            readBatcher.LinkTo(transformerBlock, new DataflowLinkOptions { PropagateCompletion = true }); 
            var inserterBlock = new ActionBlock<IEnumerable<BsonDocument>>(x =>
            {
                Debug.WriteLine($"Inserting batch {batchesInserted + 1} [{x.Count()}]");
                rawEventsCollection.Records.InsertMany(x);
                Interlocked.Increment(ref batchesInserted);
                Debug.WriteLine($"Inserted batch {batchesInserted}");
            }, executionOptions);
            transformerBlock.LinkTo(inserterBlock, new DataflowLinkOptions { PropagateCompletion = true });
            var result = await _harvester.ReadAll(readBatcher);
            _harvester = null;
            await Task.WhenAll(inserterBlock.Completion, transformerBlock.Completion);
            foreach (var index in _options.IndexesToCreate)
            {
                rawEventsCollection.EnsureIndex(index);
            }
            var output = new DataImportResult(result, rawEventsCollection);
            return output;
        }

    }
}
