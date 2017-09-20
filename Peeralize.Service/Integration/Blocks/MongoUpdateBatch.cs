using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Driver;
using nvoid.db.DB.MongoDB;

namespace Peeralize.Service.Integration.Blocks
{
    public class MongoUpdateBatch<TRecord>
    {
        private BatchBlock<FindAndModifyArgs<TRecord>> _block;
        public BatchBlock<FindAndModifyArgs<TRecord>> Block => _block;
        private CancellationToken _cancellationToken;
        private IMongoCollection<TRecord> _collection;

        public MongoUpdateBatch(IMongoCollection<TRecord> collection, int batchSize = 10000, CancellationToken? cancellationToken = null)
        {
            _block = new BatchBlock<FindAndModifyArgs<TRecord>>(batchSize);
            _block.LinkTo(new ActionBlock<FindAndModifyArgs<TRecord>[]>(UpdateAll), new DataflowLinkOptions {  PropagateCompletion =true});
            _collection = collection;
            _cancellationToken = cancellationToken == null ? CancellationToken.None : cancellationToken.Value;
        }

        private Task UpdateAll(FindAndModifyArgs<TRecord>[] modifications)
        {
            var updateModels = new WriteModel<TRecord>[modifications.Length];
            for(var i=0; i<modifications.Length; i++)
            {
                var mod = modifications[i];
                //_collection.FindAndModify(mod);
                var actionModel = new UpdateOneModel<TRecord>(mod.Query, mod.Update);
                updateModels[i] = actionModel;
            }
            return _collection.BulkWriteAsync(updateModels, new BulkWriteOptions()
            {

            }, _cancellationToken);
        }
    }
}