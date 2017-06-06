using System.Collections.Generic;
using System.Threading.Tasks;
using nvoid.db.DB;
using Peeralize.Service.Integration;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;

namespace Peeralize.Service
{
    /// <summary>
    /// Data integration handler.
    /// </summary>
    public class Harvester : Entity
    {
        public HashSet<IntegrationSet> Sets { get; private set; } 
        public IIntegrationDestination Destination { get; private set; }
        public int ThreadCount { get; private set; }

        public Harvester(int threadCount = 4) : base()
        {
            Sets = new HashSet<IntegrationSet>();
            this.ThreadCount = threadCount;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input">Details about the type</param>
        /// <param name="source">The source from which to pull the input</param>
        public Harvester AddType(IIntegrationTypeDefinition input, IInputSource source)
        {
            var newSet = new IntegrationSet(input, source);
            Sets.Add(newSet);
            return this;
        }
        /// <summary>
        /// Set the destination to which 
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public Harvester SetDestination(IIntegrationDestination dest)
        {
            Destination = dest;
            return this;
        }
        /// <summary>
        /// Performs the synchronization
        /// </summary>
        public void Synchronize()
        { 
            Destination.Consume();
            int typeCount = Sets.Count;
            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = ThreadCount };
            Parallel.ForEach(Sets, parallelOptions,
                (IntegrationSet itemSet, ParallelLoopState state) =>
                {  
                    IntegratedDocument item;
                    while(null != (item = itemSet.Read()))
                    {
                        Destination.Post(item);
                    }
                    
                });
            Destination.Close();
        }
         
    }
}
