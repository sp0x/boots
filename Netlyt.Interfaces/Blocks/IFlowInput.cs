using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Netlyt.Interfaces.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFlowInput<T>
    {
        void Post(T item);
        Task<bool> SendAsync(T item);
        BufferBlock<T> GetInputBlock();
        ITargetBlock<T> GetProcessingBlock();
    }
}