using System.Threading.Tasks.Dataflow;
using Netlyt.Interfaces.Blocks;

namespace Donut.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public class TransformFlowBlock<TIn, TOut>
        : BaseFlowBlock<TIn, TOut>
    { 

        public TransformFlowBlock(IPropagatorBlock<TIn, TOut> propagator,
            int threadCount = 4,
            int capacity = 1000) : base(procType: BlockType.Transform, threadCount: threadCount, capacity: capacity)
        {
            SetTransform(propagator, null);
        }
        protected override TOut OnBlockReceived(TIn intDoc)
        {
            return default(TOut);
        }
    }
}