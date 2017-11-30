using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Netlyt.Service.Integration.Blocks
{
    /// <summary>
    /// The base for a generic flow block, which can act as an act or transform block.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public abstract class BaseFlowBlock<TIn, TOut> 
        : IIntegrationBlock<TIn, TOut>
    {

        private BufferBlock<TIn> _buffer;
        private TransformBlock<TIn, TOut> _transformer;
        private ActionBlock<TIn> _actionBlock;
        private List<Task> _linkedBlockCompletions;
        private int _id;
        private static int __id;
        private ITargetBlock<TOut> _transformerOnCompletion;
        private IPropagatorBlock<TOut, TOut> _lastTransformerBlockOnCompletion;

        #region Props

        public ProcessingType ProcType { get; protected set; }
        public string UserId { get; set; }
        public int Id => _id;
        public Task BufferCompletion => _buffer.Completion;
        public Task ProcessingCompletion {
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
        public int ThreadId => Thread.CurrentThread.ManagedThreadId;
        #endregion

        #region Events

        protected event Action Completed;
        protected event Action<TOut> ItemProcessed;
        
        #endregion

        public BaseFlowBlock(int capacity = 1000, ProcessingType procType = ProcessingType.Transform,
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
            _buffer = new BufferBlock<TIn>(options);
            var ops = new DataflowLinkOptions { PropagateCompletion = true };
            var executionBlockOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = threadCount,
                BoundedCapacity = capacity
            };
            switch (procType)
            {
                case ProcessingType.Action:
                    _actionBlock = new ActionBlock<TIn>((item) =>
                    {
                        var receivedResult = OnBlockReceived(item);
                        //So that we can actually make a link
                        ItemProcessed?.Invoke(receivedResult);
                    }, executionBlockOptions);
                    _buffer.LinkTo(_actionBlock, ops);

                    break;
                case ProcessingType.Transform:
                    _transformer = new TransformBlock<TIn, TOut>(
                        new Func<TIn, TOut>(OnBlockReceived), executionBlockOptions);
                    _buffer.LinkTo(_transformer, ops);
                    break;
                default:
                    throw new Exception("Not supported");
            }
        }

        #region Abstract methods 
        protected abstract TOut OnBlockReceived(TIn intDoc); 
        #endregion


        #region Methods

        #region "Input"

        /// <summary>
        /// Adds the item to the sink
        /// </summary>
        /// <param name="item"></param>
        public void Post(TIn item)
        {
            DataflowBlock.Post(_buffer, item);
        }


        public Task<bool> SendAsync(TIn item)
        {
            return DataflowBlock.SendAsync(_buffer, item);
        }
        
        public void PostAll(IEnumerable<TIn> valueCollection, bool completeOnDone = true)
        {
            foreach (var elem in valueCollection)
            {
                SendAsync(elem).Wait();
            }
            if (completeOnDone)
            {
                _buffer.Complete();
            }
        }
        #endregion

        /// <summary>
        /// Gets the behaviour submission block
        /// </summary>
        /// <returns></returns>
        public ITargetBlock<TIn> GetProcessingBlock()
        {
            switch (this.ProcType)
            {
                case ProcessingType.Transform:
                    return _transformer; 
                case ProcessingType.Action:
                    return _actionBlock; 
                default:
                    throw new Exception("No task for this type of processing block.");
            }
        }
        public BufferBlock<TIn> GetBuffer()
        {
            return _buffer;
        }
        
        #region Completion
        
        public virtual void Complete()
        {
            _buffer.Complete();
            //Commented out, due to actions completing only after all data is processed and buffer propagates a completion!
            //otherwise this causes bugs.
            //            if (_transformer != null) _transformer.Complete();
            //            else
            //            {
            //                if (_actionBlock != null) _actionBlock.Complete();
            //            }
            Completed?.Invoke();
        }

        public void Fault(AggregateException objException)
        {
            var actionb = this.GetProcessingBlock();
            actionb.Fault(objException);
            //Maybe pass to the buffer that we faulted?
        }
        /// <summary>
        /// Add a task after the processing block is complete
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual IIntegrationBlock ContinueWith(Action<Task> action)
        {
            var actionBlock = GetProcessingBlock();
            actionBlock.Completion.ContinueWith(action);
            //_transformer.Completion.ContinueWith(action);
            return this;
        }

        private void HandleCompletion(Task obj)
        {
            //Link to null, so that the flow completes
            if (_lastTransformerBlockOnCompletion != null)
            {
                var nullt = DataflowBlock.NullTarget<TOut>();
                _lastTransformerBlockOnCompletion.LinkTo(nullt);
            }
            if (_transformerOnCompletion != null)
            {
                IEnumerable<TOut> elements = GetCollectedItems();
                if (elements != null)
                {
                    //Post all items to the first transformer
                    foreach (TOut element in elements)
                    {
                        if (element == null) continue;
                        _transformerOnCompletion.SendAsync(element).Wait();
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
            var tasksToWaitFor = new List<Task>();
            tasksToWaitFor.Add(ProcessingCompletion);
            tasksToWaitFor.AddRange(_linkedBlockCompletions.ToArray());
            return Task.WhenAll(tasksToWaitFor);
        }

        public void AddFlowCompletionTask(Task blockBCompletion)
        {
            _linkedBlockCompletions.Add(blockBCompletion);
        }
        public Task Completion => FlowCompletion();
        #endregion

        #region Linking

        public IIntegrationBlock ContinueWith(Action<BaseFlowBlock<TIn, TOut>> action)
        {
            _linkedBlockCompletions.Add(new Task(new Action(() =>
            {
                action(this);
            })));
            return this;
        }

        /// <summary>
        /// Link to this block when it completes
        /// N-th links are linked to the previous block.
        /// </summary>
        /// <param name="actionBlock"></param>
        /// <param name="options"></param>
        public void LinkOnComplete(IPropagatorBlock<TOut, TOut> actionBlock,
            DataflowLinkOptions options = null)
        {
            if (_transformerOnCompletion == null)
            {
                _transformerOnCompletion = actionBlock;
            }
            else
            {
                if (options == null) options = new DataflowLinkOptions() { PropagateCompletion = true };

                //Make sure to link the next block to this one, instead of the first one ever added. 0
                _lastTransformerBlockOnCompletion.LinkTo(actionBlock, options);
            }
            _lastTransformerBlockOnCompletion = actionBlock;
            _linkedBlockCompletions.Add(actionBlock.Completion);
        }
        /// <summary>
        /// Posts all items to the given block, upon the full completion of the current one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actionBlock">Note: Ensure that you link the block to an output target. There is no automatic null-target block linking.</param>
        public void LinkOnCompleteEx<T>(IPropagatorBlock<TOut, T> actionBlock)
        {
            var standardTransformer = new TransformBlock<TOut, TOut>((doc) =>
            {
                actionBlock.SendAsync(doc).Wait();
                return doc;
            }, new ExecutionDataflowBlockOptions()
            {
            });
            standardTransformer.Completion.ContinueWith((Task t) =>
            {
                actionBlock.Complete();
            });
            LinkOnComplete(standardTransformer as IPropagatorBlock<TOut, TOut>);
        }
        /// <summary>
        /// Link this block to another, when it completes.
        /// N-th links are linked to the previous block.
        /// Non propagation targets(Actions blocks) are proxied and their input  is used as output after invoking.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="options"></param>
        public void LinkOnComplete(BaseFlowBlock<TOut, TOut> target,
            DataflowLinkOptions options = null)
        {
            var targetBuffer = target.GetBuffer();
            var targetAction = target.GetProcessingBlock();
            if (options == null) options = new DataflowLinkOptions() { PropagateCompletion = true };
            //Determine the block that we'll be waiting for, since we're posting to a buffer, linked to an action block
            //use that block to link any secondary blocks to it!
            IPropagatorBlock<TOut, TOut> targetOutputBlock = null;
            switch (target.ProcType)
            {
                case ProcessingType.Action:
                    //Create a blank transformer, and link the target to it
                    targetOutputBlock = new TransformBlock<TOut, TOut>(x =>
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
        public IIntegrationBlock BroadcastTo(ITargetBlock<TOut> target, bool linkToCompletion = true)
        {
            ItemProcessed += (x) =>
            {
                DataflowBlock.SendAsync(target, x).Wait();
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
        public IIntegrationBlock LinkTo(ITargetBlock<TOut> targetBlock, DataflowLinkOptions linkOptions = null)
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
        public IIntegrationBlock LinkTo(IIntegrationBlock<TOut, TOut> target, DataflowLinkOptions linkOptions = null)
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

                linkOptions = new DataflowLinkOptions() { PropagateCompletion = true };
                if (_transformer != null)
                {
                    if (linkOptions != null)
                    {
                        _transformer.LinkTo(targetBuffer, linkOptions);
                    }
                    else
                    {
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

        protected virtual IEnumerable<TOut> GetCollectedItems()
        {
            return null;
        }

        #endregion

    }
}