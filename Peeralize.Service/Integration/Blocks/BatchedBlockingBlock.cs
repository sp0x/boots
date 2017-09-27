using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;

namespace Peeralize.Service.Integration.Blocks
{
    public abstract class BatchedBlockingBlock<T>
    {
        public static IPropagatorBlock<T, T[]> CreateBlock(int batchSize)
        { 
            var executionOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 1,
            }; 
            var destination = new BufferBlock<T[]>(executionOptions); 
            Queue<T> queue = new Queue<T>();
            var queuer = new ActionBlock<T>(x =>
            {
                if (queue.Count >= batchSize)
                {
                    var element = queue.ToArray();
                    queue.Clear();
                    destination.SendChecked(element);
                }
                queue.Enqueue(x);
            }, new ExecutionDataflowBlockOptions { BoundedCapacity = batchSize, MaxDegreeOfParallelism = 1 });
            //insertBat.LinkTo(transformerBlock, new DataflowLinkOptions { PropagateCompletion = true });
            queuer.Completion.ContinueWith(x =>
            {
                var element = queue.ToArray();
                queue.Clear();
                destination.SendChecked(element);
                destination.Complete();
            });
            var outputBlock = DataflowBlock.Encapsulate(queuer, destination);
            return outputBlock;
        }
    }
}