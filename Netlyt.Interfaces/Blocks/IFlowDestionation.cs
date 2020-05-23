using System.Threading.Tasks.Dataflow;

namespace Netlyt.Interfaces.Blocks
{


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFlowDestionation<T>
    {
        IFlowBlock LinkTo(ITargetBlock<T> targetBlock, DataflowLinkOptions linkOptions = null);
    }
}