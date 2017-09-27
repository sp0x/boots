using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Peeralize.Service.Integration
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
    }
}
