using System;

namespace Netlyt.Interfaces.Blocks
{
    public interface IFlowBlock : IBufferedProcessingBlock
    {
        BlockType ProcType { get; }
        string AppId { get; set; }
        void Complete();
        //IFlowBlock ContinueWith(Action<Task> action);
        IFlowBlock ContinueWith(Action action);
        void Fault(AggregateException objException);
    }

    public interface IFlowBlock<TIn>
        : IFlowBlock,
            IFlowInput<TIn>
    {
    }

    public interface IFlowBlock<TIn, TOut> 
        : IFlowBlock<TIn>,
            IFlowDestionation<TOut>
    {
    }
}