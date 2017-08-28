using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using nvoid.db.DB;
using Peeralize.Service.Integration;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;

namespace Peeralize.Service
{
    /// <summary>
    /// Data integration handler.
    /// Handles type, sourse and destination piping, to control the integration data flow with multiple block scenarios.
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
        public Harvester AddType(IIntegrationTypeDefinition input, InputSource source)
        {
            if (input == null) throw new ArgumentException(nameof(input));
            var newSet = new IntegrationSet(input, source);
            Sets.Add(newSet);
            return this;
        }

        /// <summary>
        /// Set the destination to which to push all data.
        /// </summary>
        /// <param name="dest">A destination/block in your integration flow</param>
        /// <returns></returns>
        public Harvester SetDestination(IIntegrationDestination dest)
        {
            Destination = dest;
            return this;
        }

        /// <summary>
        /// Starts reading all the sets that are available
        /// </summary>
        public void Synchronize()
        { 
            Destination.ConsumeAsync(CancellationToken.None);
            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = ThreadCount };
            //Go through all type sets
            Parallel.ForEach(Sets, parallelOptions,
                (IntegrationSet itemSet, ParallelLoopState state) =>
                {   
                    if (itemSet.Source.SupportsSeeking)
                    {
                        Parallel.ForEach(itemSet.AsEnumerable(), (doc) =>
                        {
                            Destination.Post(doc);
                        });
                    }
                    else
                    {
                        Parallel.ForEach(itemSet.Source.Shards(), (InputSource sourceShard) =>
                        {
                            dynamic entry;
                            FileSource fsShard = sourceShard as FileSource;
                            Debug.WriteLine($"Reading file: {fsShard.Path}");
                            while ((entry = sourceShard.GetNext()) !=null)
                            {
                                var document = itemSet.Wrap(entry);
                                Destination.Post(document);
                            }
                        });
                    } 
            });
            Destination.Close();
        }
         
    }
}
