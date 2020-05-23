using System.Threading.Tasks;

namespace Netlyt.Interfaces.Blocks
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
}