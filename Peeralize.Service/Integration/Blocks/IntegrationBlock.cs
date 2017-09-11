using System;
using System.Collections.Generic;
using System.Linq;
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
        #region Vars
        private BufferBlock<IntegratedDocument> _buffer;
        private TransformBlock<IntegratedDocument, IntegratedDocument> _transformer;
        private ActionBlock<IntegratedDocument> _actionBlock;
        private List<Task> _linkedBlockCompletions;
        //private IPropagatorBlock<IntegratedDocument, IntegratedDocument> _transformerOnCompletion;
        private ITargetBlock<IntegratedDocument> _transformerOnCompletion;
        private IPropagatorBlock<IntegratedDocument, IntegratedDocument> _lastTransformerBlockOnCompletion;
        //private ITargetBlock<IntegratedDocument> _lastLinkedBlock;
        private int _id;
        private static int __id;
        #endregion

        #region Props
        public ProcessingType ProcType { get; private set; }
        public Task ProcessingCompletion
        {
            get
            {
                Task completion = null;
                switch (this.ProcType)
                {
                    case ProcessingType.Transform:
                        completion = _transformer?.Completion;
                        break;
                    case ProcessingType.Action:
                        completion = _actionBlock?.Completion;
                        break;
                    default:
                        throw new Exception("No task for this type of processing block.");
                }
                completion = completion.ContinueWith(HandleCompletion);
                return completion;
            }
        } 

        public Task BufferCompletion => _buffer.Completion;
        public int Id => _id;
        public string UserId { get; protected set; }
        public int ThreadId => Thread.CurrentThread.ManagedThreadId;
        #endregion

        #region Events 
        protected event Action Completed;
        protected event Action<dynamic> ItemProcessed;
        #endregion




        public IntegrationBlock(int capacity = 1000 * 1000, 
            ProcessingType procType = ProcessingType.Transform,
            int threadCount = 4)
        {
            _id = __id;
            Interlocked.Increment(ref __id);
            _linkedBlockCompletions = new List<Task>(); 
            ProcType = procType;
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
            switch (procType)
            {
                case ProcessingType.Action:
                    _actionBlock = new ActionBlock<IntegratedDocument>((item) =>
                    {
                        var receivedResult = OnBlockReceived(item);
                        //So that we can actually make a link
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
                    throw new Exception("Not supported");
            }

        } 
        /// <summary>
        /// Gets the behaviour submission block
        /// </summary>
        /// <returns></returns>
        public ITargetBlock<IntegratedDocument> GetProcessingBlock()
        {
            switch (this.ProcType)
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

        public BufferBlock<IntegratedDocument> GetBuffer()
        {
            return _buffer;
        }
         

        /// <summary>
        /// Consumes available integration documents.
        /// @Deprecated
        /// </summary>
        public async void ConsumeAsync(CancellationToken token)
        {
//            if (UserId == null)
//                throw new InvalidOperationException("User id must be set in order to push data to this destination.");
//            while (await DataflowBlock.OutputAvailableAsync<IntegratedDocument>(_buffer, token))
//            {
//                Console.WriteLine($@"{DateTime.Now}: Current load: {_buffer.Count} items");
//            }
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

        public virtual void Complete()
        {
            _buffer.Complete();
            //if(_transformer!=null) _transformer.Complete();
            //if(_actionBlock!=null) _actionBlock.Complete();
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


        public void PostAll(IEnumerable<IntegratedDocument> valueCollection, bool completeOnDone = true)
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


#region Completion
        private void HandleCompletion(Task obj)
        {
            //Link to null, so that the flow completes
            if (_lastTransformerBlockOnCompletion != null)
            {
                var nullt = DataflowBlock.NullTarget<IntegratedDocument>();
                _lastTransformerBlockOnCompletion.LinkTo(nullt);
            }
            if (_transformerOnCompletion != null)
            { 
                var elements = GetCollectedItems();
                if (elements!=null)
                {
                    //Post all items to the first transformer
                    foreach (var element in elements)
                    {
                        if (element == null) continue;
                        _transformerOnCompletion.Post(element);
                    }
                }
                //Set it to complete
                _transformerOnCompletion.Complete();
                //Wait for the last transformer to complete
                _lastTransformerBlockOnCompletion.Completion.Wait();
            }
        }

        /// <summary>
        /// Gets the task of all the child blocks completing + the current one.
        /// </summary>
        /// <returns></returns>
        public Task FlowCompletion()
        {
            var tasksToWaitFor = new List<Task>(_linkedBlockCompletions.ToArray());
            tasksToWaitFor.Add(ProcessingCompletion);
            return Task.WhenAll(tasksToWaitFor);
        }
#endregion


        #region Linking



        /// <summary>
        /// Link to this block when it completes
        /// N-th links are linked to the previous block.
        /// </summary>
        /// <param name="actionBlock"></param>
        /// <param name="options"></param>
        public void LinkOnComplete(IPropagatorBlock<IntegratedDocument, IntegratedDocument> actionBlock,
            DataflowLinkOptions options = null)
        {
            if (_transformerOnCompletion == null)
            {
                _transformerOnCompletion = actionBlock;
            }
            else
            {
                if (options == null) options = new DataflowLinkOptions() {PropagateCompletion = true};
                
                //Make sure to link the next block to this one, instead of the first one ever added. 0
                _lastTransformerBlockOnCompletion.LinkTo(actionBlock, options);
            }
            _lastTransformerBlockOnCompletion = actionBlock;
            _linkedBlockCompletions.Add(actionBlock.Completion);
        }
        public IIntegrationDestination ContinueWith(Action<IntegrationBlock> action)
        { 
            _linkedBlockCompletions.Add(new Task(new Action(() =>
            {
                action(this);
            })));
            return this;
        }
        /// <summary>
        /// Link this block to another, when it completes.
        /// N-th links are linked to the previous block.
        /// Non propagation targets(Actions blocks) are proxied and their input  is used as output after invoking.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="options"></param>
        public void LinkOnComplete(IntegrationBlock target,
            DataflowLinkOptions options = null)
        {
            var targetBuffer = target.GetBuffer();
            var targetAction = target.GetProcessingBlock();
            if (options == null) options = new DataflowLinkOptions() { PropagateCompletion = true };
            //Determine the block that we'll be waiting for, since we're posting to a buffer, linked to an action block
            //use that block to link any secondary blocks to it!
            IPropagatorBlock<IntegratedDocument, IntegratedDocument> targetOutputBlock = null;
            switch (target.ProcType)
            {
                case ProcessingType.Action:
                    //Create a blank transformer, and link the target to it
                    targetOutputBlock = new TransformBlock<IntegratedDocument, IntegratedDocument>(x =>
                    { 
                        return x;
                    });
                    target.LinkTo(targetOutputBlock);
                    
                    
                    break;
                case ProcessingType.Transform:
                    targetOutputBlock = target._transformer;
                    break;
            }
            //First link, link to the target's buffer
            if (_transformerOnCompletion == null)
            {
                _transformerOnCompletion = targetBuffer;
            }
            else
            {
                _lastTransformerBlockOnCompletion.LinkTo(targetBuffer, options);
            }
            //So that the next block can link to the output of the previous
            _lastTransformerBlockOnCompletion = targetOutputBlock;
             
            _linkedBlockCompletions.Add(target.FlowCompletion());
        }
        /// <summary>
        /// Links an action to this block. Usefull if it's an action block.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param> 
        /// <returns></returns>
        public IIntegrationDestination BroadcastTo<T>(T target, bool linkToCompletion = true)
            where T : ITargetBlock<IntegratedDocument>
        {
            ItemProcessed += (x) =>
            {
                DataflowBlock.Post(target, x);
            };
            if (linkToCompletion)
            {
                _linkedBlockCompletions.Add(target.Completion);
            }
            var actionBlock = this.GetProcessingBlock();
            actionBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) target.Fault(t.Exception);
                else target.Complete();
            });
            //_lastLinkedBlock = target;
            return this;
        }


        /// <summary>
        /// Links to the given target block.
        /// Beware that FlowCompletion would wait for the completion of the given block.
        /// </summary>
        /// <param name="targetBlock"></param>
        /// <param name="linkOptions"></param>
        /// <returns></returns>
        public IIntegrationDestination LinkTo(ITargetBlock<IntegratedDocument> targetBlock,
            DataflowLinkOptions linkOptions = null)
        {
            if (_transformer != null)
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
                _linkedBlockCompletions.Add(targetBlock.Completion);
            }
            else
            { 
                BroadcastTo(targetBlock);
                //throw new Exception("Can't link blocks which don`t produce data! Use " + nameof(LinkTo) + " instead!");
            }
            return this;
        }

        /// <summary>
        /// Links this block to a next one.
        /// If this block is an action block, it acts as a BroadcastBlock to all linked children.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="linkOptions"></param>
        /// <returns></returns>
        public IIntegrationDestination LinkTo(IntegrationBlock target, DataflowLinkOptions linkOptions = null)
        {
            var targetBuffer = target.GetBuffer();
            if (!string.IsNullOrEmpty(UserId))
            {
                target.UserId = UserId;
            }
            //var targetAction = target.GetProcessingBlock();
            if (this.ProcType == ProcessingType.Action)
            {
                BroadcastTo(targetBuffer, false);
            }
            else if (this.ProcType == ProcessingType.Transform)
            {
                if (_transformer != null)
                {
                    if (linkOptions != null)
                    {
                        _transformer.LinkTo(targetBuffer, linkOptions);
                    }
                    else
                    {
                        linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };
                        _transformer.LinkTo(targetBuffer, linkOptions);
                    }
                }
                else
                {
                    throw new Exception("Can't link blocks which don`t produce data!");
                }
            }
            var actionBlock = this.GetProcessingBlock();
            actionBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) target.Fault(t.Exception);
                else target.Complete();
            });
            //_lastLinkedBlock = targetAction;
            _linkedBlockCompletions.Add(target.FlowCompletion());

            return this;
        }
        #endregion

        private void Fault(AggregateException objException)
        {
            var actionb = this.GetProcessingBlock();
            actionb.Fault(objException);
            //Maybe pass to the buffer that we faulted?
        }
        public IIntegrationDestination ContinueWith(Action<Task> action)
        {
            var actionBlock = GetProcessingBlock();
            actionBlock.Completion.ContinueWith(action);
            //_transformer.Completion.ContinueWith(action);
            return this;
        }

        protected virtual IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return null;
        }
        

        protected abstract IntegratedDocument OnBlockReceived(IntegratedDocument intDoc);

         
    }
}