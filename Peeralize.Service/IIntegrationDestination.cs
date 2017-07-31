using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;

namespace Peeralize.Service
{
    public interface IIntegrationDestination
    {
        /// <summary>
        /// Consumes any available documents, and blocks.
        /// </summary>
        void Consume();
        /// <summary>
        /// Consumes any available documents async.
        /// </summary>
        void ConsumeAsync(CancellationToken token);
        void Close();
        void Post(IntegratedDocument item);
        IIntegrationDestination LinkTo(ITargetBlock<IntegratedDocument> targetBlock, DataflowLinkOptions linkOptions = null);
        IIntegrationDestination LinkTo(IntegrationBlock targetDestination, DataflowLinkOptions linkOptions = null);
        ITargetBlock<IntegratedDocument> GetActionBlock();
        IIntegrationDestination ContinueWith(Action<Task> action);

        string UserId { get; }

    }
}