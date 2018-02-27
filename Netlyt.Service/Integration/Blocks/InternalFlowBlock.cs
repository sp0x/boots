using System;
using System.Threading.Tasks.Dataflow;
using nvoid.exec.Blocks;

namespace Netlyt.Service.Integration.Blocks
{
    public class InternalFlowBlock<TIn, TOut>
        : BaseFlowBlock<TIn, TOut>
    { 

        public InternalFlowBlock(IPropagatorBlock<TIn, TOut> func,
            int threadCount = 4,
            int capacity = 1000) : base(procType: BlockType.Transform, threadCount: threadCount, capacity: capacity)
        {
            SetTransform(func, null);
        }
        protected override TOut OnBlockReceived(TIn intDoc)
        {
            return default(TOut);
        }
    }
}