using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Driver;
using nvoid.db.DB.MongoDB;

namespace Peeralize.Service.Integration.Blocks
{
    public class MongoInsertBatch<TRecord>
    {
        private BatchBlock<TRecord> _block;
        public BatchBlock<TRecord> Block => _block;
        private CancellationToken _cancellationToken;
        private IMongoCollection<TRecord> _collection;
        /// <summary>
        /// Full import completion task
        /// </summary>
        public Task Completion => _actionBlock?.Completion;
        private readonly ActionBlock<TRecord[]> _actionBlock;

        public MongoInsertBatch(IMongoCollection<TRecord> collection, int batchSize = 10000, CancellationToken? cancellationToken = null)
        {
            _block = new BatchBlock<TRecord>(batchSize);
            _actionBlock = new ActionBlock<TRecord[]>(InsertAll);
            _block.LinkTo(_actionBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _collection = collection;
            _cancellationToken = cancellationToken == null ? CancellationToken.None : cancellationToken.Value;
        }

        private Task InsertAll(TRecord[] newModels)
        {
            var updateModels = new WriteModel<TRecord>[newModels.Length];
            for (var i = 0; i < newModels.Length; i++)
            {
                var mod = newModels[i];
                //_collection.FindAndModify(mod);
                var actionModel = new InsertOneModel<TRecord>(mod);
                updateModels[i] = actionModel;
            }
            var output = _collection.BulkWriteAsync(updateModels, new BulkWriteOptions()
            {

            }, _cancellationToken).ContinueWith(x =>
            {
                Debug.WriteLine($"{DateTime.Now} Written batch[{newModels.Length}]");
            }, _cancellationToken);
            return output;
        }
    }
}