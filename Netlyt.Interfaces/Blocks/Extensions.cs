using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Netlyt.Interfaces.Blocks
{
    public static class Extensions
    {
        public static void SendChecked<T>(this ITargetBlock<T> block, T data, Func<bool> predicate = null)
        {
            Task<bool> sendTask = null;
            do
            {
                if (predicate != null)
                {
                    if (predicate()) break;
                }
                sendTask = block.SendAsync(data);
                sendTask.Wait();
            } while (!sendTask.IsCompleted || !sendTask.Result);
        }

        public static void LinkToEnd<T>(this ISourceBlock<T> block, DataflowLinkOptions linkOptions = null)
        {
            if (linkOptions == null)
            {
                block.LinkTo(DataflowBlock.NullTarget<T>());
            }
            else
            {
                block.LinkTo(DataflowBlock.NullTarget<T>(), linkOptions);
            }
        }
    }
}
