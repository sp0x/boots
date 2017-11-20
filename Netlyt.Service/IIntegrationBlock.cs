using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;

namespace Netlyt.Service
{
    public interface IBufferedProcessingBlock
    {
        /// <summary>
        /// The completion task for all the buffering.
        /// </summary>
        Task BufferCompletion { get; }
        /// <summary>
        /// The completion task for all the processing.
        /// </summary>
        Task ProcessingCompletion { get; }

        Task FlowCompletion();
    }

    public interface IIntegrationBlock : IBufferedProcessingBlock
    {
        ProcessingType ProcType { get; }
        string UserId { get; set; }
        void Complete();
        IIntegrationBlock ContinueWith(Action<Task> action);
        void Fault(AggregateException objException);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IIntegrationDestination<T>
    {
        IIntegrationBlock LinkTo(ITargetBlock<T> targetBlock, DataflowLinkOptions linkOptions = null);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IIntegrationInput<T>
    {
        void Post(T item);
        Task<bool> SendAsync(T item);
        BufferBlock<T> GetBuffer();
        ITargetBlock<T> GetProcessingBlock();
    }
    public interface IIntegrationBlock<TIn>
        : IIntegrationBlock, 
          IIntegrationInput<TIn>
    {
    }
    public interface IIntegrationBlock<TIn, TOut> 
        : IIntegrationBlock<TIn>, 
        IIntegrationDestination<TOut>
    {
    }
}