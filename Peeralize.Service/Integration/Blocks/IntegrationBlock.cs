using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;
using Peeralize.Service.IntegrationSource;

namespace Peeralize.Service.Integration.Blocks
{
    public enum ProcessingType
    {
        Transform, Action
    }
    /// <summary>
    /// A block in your integration flow
    /// </summary>
    public abstract class IntegrationBlock : IIntegrationDestination
    {

        private BufferBlock<IntegratedDocument> _buffer;
        private TransformBlock<IntegratedDocument, IntegratedDocument> _transformer;
        private ActionBlock<IntegratedDocument> _actionBlock;

        public ProcessingType ProcessingType { get; private set; }

        protected event Action Completed;

        public Task BufferCompletion => _buffer.Completion;
        public Task ProcessingCompletion
        {
            get
            {
                switch (this.ProcessingType)
                {
                    case ProcessingType.Transform:
                        return _transformer?.Completion;
                        break;
                    case ProcessingType.Action:
                        return _actionBlock?.Completion;
                        break;
                    default:
                        throw new Exception("No task for this type of processing block.");
                }
            }
        }

        public string UserId { get; protected set; }

        protected event Action<dynamic> ItemProcessed;

        public IntegrationBlock(int capacity = 1000 * 1000, 
            ProcessingType processingType = ProcessingType.Transform,
            int threadCount = 4)
        {
            this.ProcessingType = processingType;
            var options = new DataflowBlockOptions()
            {
                BoundedCapacity = capacity,
                EnsureOrdered = true
            };
            _buffer = new BufferBlock<IntegratedDocument>(options);
            var ops = new DataflowLinkOptions
            { PropagateCompletion = true};
            var executionBlockOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = threadCount
            };
            switch (processingType)
            {
                case ProcessingType.Action:
                    _actionBlock = new ActionBlock<IntegratedDocument>((item) =>
                    {
                        var receivedResult = OnBlockReceived(item);
                        ItemProcessed?.Invoke(receivedResult);
                    }, executionBlockOptions);
                    _buffer.LinkTo(_actionBlock, ops);

                    break;
                case ProcessingType.Transform:
                    _transformer = new TransformBlock<IntegratedDocument, IntegratedDocument>(
                        new Func<IntegratedDocument, IntegratedDocument>(OnBlockReceived), executionBlockOptions);
                    _buffer.LinkTo(_transformer, ops);
                    break;
                default:
                    throw new Exception("Not suppo");
            }

        } 


        /// <summary>
        /// Gets the behaviour submission block
        /// </summary>
        /// <returns></returns>
        public ITargetBlock<IntegratedDocument> GetProcessingBlock()
        {
            switch (this.ProcessingType)
            {
                case ProcessingType.Transform:
                    return _transformer;
                    break;
                case ProcessingType.Action:
                    return _actionBlock;
                    break;
                default:
                    throw new Exception("No task for this type of processing block.");
            }
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
            if(_transformer!=null) _transformer.Complete();
            if(_actionBlock!=null) _actionBlock.Complete();
            Completed?.Invoke();
        }


        #region "Input"
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
        #endregion


        public IIntegrationDestination LinkTo(ITargetBlock<IntegratedDocument> targetBlock, DataflowLinkOptions linkOptions = null)
        { 
            if (_transformer != null)
            {

                if (linkOptions != null)
                {
                    _transformer.LinkTo(targetBlock, linkOptions);
                }
                else
                {
                    linkOptions = new DataflowLinkOptions() {PropagateCompletion = true};
                    _transformer.LinkTo(targetBlock, linkOptions);
                }
            }
            else
            { 
                throw new Exception("Can't link blocks which don`t produce data! Use " + nameof(LinkAction) + " instead!");
            }
            return this;
        }

        /// <summary>
        /// Links an action to this block. Usefull if it's an action block.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="linkOptions"></param>
        /// <returns></returns>
        public IIntegrationDestination LinkAction<T>(ITargetBlock<T> target)
            where T : class
        {
            ItemProcessed += (x) => target.Post(x as T);
            return this;
        }

        public IIntegrationDestination LinkTo(IntegrationBlock target, DataflowLinkOptions linkOptions = null)
        {
            var actionBlock = target.GetProcessingBlock();
            if (_transformer != null)
            {
                if (linkOptions != null)
                {
                    _transformer.LinkTo(actionBlock, linkOptions);
                }
                else
                {
                    linkOptions = new DataflowLinkOptions() {PropagateCompletion = true};
                    _transformer.LinkTo(actionBlock, linkOptions);
                }
            }
            else
            {
                throw new Exception("Can't link blocks which don`t produce data!");
            }
            return this;
        }

        public IIntegrationDestination ContinueWith(Action<Task> action)
        {
            var actionBlock = GetProcessingBlock();
            actionBlock.Completion.ContinueWith(action);
            //_transformer.Completion.ContinueWith(action);
            return this;
        }

        private void OnTransformComplete(Task obj)
        {
            throw new NotImplementedException();
        }

        protected abstract IntegratedDocument OnBlockReceived(IntegratedDocument intDoc);

    }
}