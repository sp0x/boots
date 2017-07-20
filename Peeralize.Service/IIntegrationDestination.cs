using System.Threading;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;

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
        IIntegrationDestination LinkTo(ITargetBlock<IntegratedDocument> behaviourContext, DataflowLinkOptions linkOptions = null);
        string UserId { get; }
    }
}