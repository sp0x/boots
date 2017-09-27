using System;
using System.Collections.Concurrent;
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
        private IPropagatorBlock<TRecord, TRecord[]> _batchBlock;
        public IPropagatorBlock<TRecord, TRecord[]> BatchBlock => _batchBlock;
        private CancellationToken _cancellationToken;
        private IMongoCollection<TRecord> _collection;
        private int _batchesSent;

        /// <summary>
        /// Full import completion task
        /// </summary>
        public Task Completion => _actionBlock.Completion;
        private readonly ActionBlock<TRecord[]> _actionBlock;

        public MongoInsertBatch(IMongoCollection<TRecord> collection, int batchSize = 1000, CancellationToken? cancellationToken = null)
        { 
            _batchesSent = 0;
            _batchBlock = BatchedBlockingBlock<TRecord>.CreateBlock(batchSize);

            _cancellationToken = cancellationToken == null ? CancellationToken.None : cancellationToken.Value;
            Func<TRecord[], Task> targetAction = InsertAll;
            _actionBlock = new ActionBlock<TRecord[]>(targetAction, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = batchSize,
                CancellationToken = _cancellationToken
            });
            _batchBlock.LinkTo(_actionBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _collection = collection;
        }

        private Task InsertAll(TRecord[] newModels)
        {
            return _collection.InsertManyAsync(newModels, null, cancellationToken: _cancellationToken).ContinueWith(x =>
            {
                Interlocked.Increment(ref _batchesSent);
                Debug.WriteLine($"{DateTime.Now} Written batch{_batchesSent} [{newModels.Length}]");
            }, _cancellationToken);
        }

        public void Trigger()
        {
            //_batchBlock.TriggerBatch();
        }

        public void Complete()
        {
            _batchBlock.Complete();
        }
    }
}