using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;
using Peeralize.Service.IntegrationSource;

namespace Peeralize.Service.Integration.Blocks
{
    /// <summary>
    /// A block in your integration flow
    /// </summary>
    public abstract class IntegrationBlock : IIntegrationDestination
    {

        private BufferBlock<IntegratedDocument> _buffer;
        private TransformBlock<IntegratedDocument, IntegratedDocument> _transformer;
        protected event Action Completed;

        public Task Completion => _transformer.Completion;

        public string UserId { get; protected set; }

        public IntegrationBlock(int capacity = 1000 * 1000)
        {

            var options = new DataflowBlockOptions()
            {
                BoundedCapacity = capacity,
                EnsureOrdered = true, 
            };
            _buffer = new BufferBlock<IntegratedDocument>(options);
            _transformer = new TransformBlock<IntegratedDocument, IntegratedDocument>(new Func<IntegratedDocument, IntegratedDocument>(OnBlockReceived));
            var ops = new DataflowLinkOptions
            {
                PropagateCompletion = true
            };
            _buffer.LinkTo(_transformer, ops);
        }



        /// <summary>
        /// Gets the behaviour submission block
        /// </summary>
        /// <returns></returns>
        public ITargetBlock<IntegratedDocument> GetActionBlock()
        {
            return _transformer;
        }



        /// <summary>
        /// Consumes available integration documents.
        /// @Deprecated
        /// </summary>
        public async void ConsumeAsync(CancellationToken token)
        {
            if (UserId == null)
                throw new InvalidOperationException("User id must be set in order to push data to this destination.");
            while (await DataflowBlock.OutputAvailableAsync<IntegratedDocument>(_buffer, token))
            {
                Console.WriteLine($@"{DateTime.Now}: Current load: {_buffer.Count} items");
            }
        }

        /// <summary>
        /// Consumes available integration documents.
        /// </summary>
        public void Consume()
        {
            if (UserId == null)
                throw new InvalidOperationException("User id must be set in order to push data to this destination.");
            while (DataflowBlock.OutputAvailableAsync<IntegratedDocument>(_buffer).Result)
            {
                Console.WriteLine($@"{DateTime.Now}: Current load: {_buffer.Count} items");
            }
        }

        public virtual void Close()
        {
            _buffer.Complete();
            _transformer.Complete();
            Completed?.Invoke();
        }

        /// <summary>
        /// Adds the item to the sink
        /// </summary>
        /// <param name="item"></param>
        public void Post(IntegratedDocument item)
        {
            DataflowBlock.Post(_buffer, item);
        }

        /// <summary>
        /// Adds the item to the sink
        /// </summary>
        /// <param name="item"></param>
        public async Task<bool> PostAsync(IntegratedDocument item)
        {
            return await DataflowBlock.SendAsync(_buffer, item);
        }


        public void PostAll(Dictionary<object, IntegratedDocument>.ValueCollection valueCollection, bool completeOnDone = true)
        {
            foreach (var elem in valueCollection)
            {
                Post(elem);
            }
            if (completeOnDone)
            {
                _buffer.Complete();
            }
        }


        public IIntegrationDestination LinkTo(ITargetBlock<IntegratedDocument> targetBlock, DataflowLinkOptions linkOptions = null)
        {
            if (linkOptions != null)
            {
                _transformer.LinkTo(targetBlock, linkOptions);
            }
            else
            {
                linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };
                _transformer.LinkTo(targetBlock, linkOptions);
            }
            return this;
        }

        public IIntegrationDestination LinkTo(IntegrationBlock target, DataflowLinkOptions linkOptions = null)
        {
            var actionBlock = target.GetActionBlock();
            if (linkOptions != null)
            {
                _transformer.LinkTo(actionBlock, linkOptions); 
            }
            else
            {
                linkOptions = new DataflowLinkOptions() {PropagateCompletion = true};
                _transformer.LinkTo(actionBlock, linkOptions);
            }
            return this;
        }

        public IIntegrationDestination ContinueWith(Action<Task> action)
        {
            _transformer.Completion.ContinueWith(action);
            return this;
        }

        private void OnTransformComplete(Task obj)
        {
            throw new NotImplementedException();
        }

        protected abstract IntegratedDocument OnBlockReceived(IntegratedDocument intDoc);

    }
}