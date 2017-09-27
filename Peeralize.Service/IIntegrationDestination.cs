using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;

namespace Peeralize.Service
{
    public interface IBufferedFrocessingBlock
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
    public interface IIntegrationDestination : IBufferedFrocessingBlock
    {
        /// <summary>
        /// Consumes any available documents, and blocks.
        /// </summary>
        void Consume();
        /// <summary>
        /// Consumes any available documents async.
        /// </summary>
        void ConsumeAsync(CancellationToken token);
        void Complete();
        void Post(IntegratedDocument item);
        Task<bool> SendAsync(IntegratedDocument item);
        IIntegrationDestination LinkTo(ITargetBlock<IntegratedDocument> targetBlock, DataflowLinkOptions linkOptions = null);
        IIntegrationDestination LinkTo(IntegrationBlock targetDestination, DataflowLinkOptions linkOptions = null);
        ITargetBlock<IntegratedDocument> GetProcessingBlock();
        IIntegrationDestination ContinueWith(Action<Task> action);
         
        string UserId { get; }

    }
}