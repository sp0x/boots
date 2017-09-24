using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nvoid.db.DB;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;

namespace Peeralize.Service
{
    public class HarvesterResult
    {
        public int ProcessedEntries { get; private set; }
        public int ProcessedShards { get; private set; }

        public HarvesterResult(int shards, int elements)
        {
            ProcessedEntries = elements;
            ProcessedShards = shards;
        }
    }

    /// <summary>
    /// Data integration handler.
    /// Handles type, sourse and destination piping, to control the integration data flow with multiple block scenarios.
    /// </summary>
    public class Harvester : Entity
    {
        private Stopwatch _stopwatch;
        private int _shardLimit;
        private int _totalEntryLimit;

        public HashSet<IntegrationSet> Sets { get; private set; } 
        public IIntegrationDestination Destination { get; private set; }

        protected int ShardLimit => _shardLimit;
        protected int TotalEntryLimit => _totalEntryLimit;
        public int ThreadCount { get; private set; }

        public Harvester(int threadCount = 4) : base()
        {
            Sets = new HashSet<IntegrationSet>();
            this.ThreadCount = threadCount;
            _stopwatch = new Stopwatch();
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
        /// Resolves the type from the input, persists it to the DB, and adds it as an integration set from the given source.
        /// </summary>
        /// <param name="inputSource"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IntegrationTypeDefinition AddPersistentType(InputSource inputSource, string userId, bool persist = true)
        {
            var type = inputSource.GetTypeDefinition() as IntegrationTypeDefinition;
            if (type == null)
            {
                throw new Exception("Could not resolve type!");
            }
            type.UserId = userId;
            if (persist)
            {
                if (!IntegrationTypeDefinition.TypeExists(type, userId, out var existingDataType)) type.SaveType(userId);
                else type = existingDataType;
            }
            AddType(type, inputSource);
            return type;
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

        private void ResetStopwatch()
        { 
            _stopwatch.Stop();
            _stopwatch.Reset();
        }

        /// <summary>
        /// Starts reading all the sets that are available.
        /// Returns whenever the whole pipeline is complete.
        /// </summary>
        public Task<HarvesterResult> Synchronize(CancellationToken? cancellationToken = null)
        {
            var cToken = cancellationToken ?? CancellationToken.None;
            Destination.ConsumeAsync(cToken);
            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = ThreadCount };
            parallelOptions.CancellationToken = cToken;

            ResetStopwatch();
            _stopwatch.Start();
            int shardsUsed;
            var totalItemsUsed = ProcessInputShards(parallelOptions, out shardsUsed);
//          Test destination buffering and processing
//          Destination.ProcessingCompletion.ContinueWith((t) =>
//          { 
//          });
//          Destination.BufferCompletion.ContinueWith((t) =>
//          { 
//          });
            //Let the dest know that we're finished passing data to it, so that it could complete.
            Destination.Complete(); 
            //_stopwatch.Stop(); 
            var flowCompletion = Destination.FlowCompletion();
            return flowCompletion.ContinueWith(continuationFunction: (t) =>
            {
                _stopwatch.Stop();
                var output = new HarvesterResult(shardsUsed, totalItemsUsed);
                return output;
            }, cancellationToken: cToken);
        }

        private int ProcessInputShards(ParallelOptions parallelOptions, out int totalShardsUsed)
        {
            var totalItemsUsed = 0;
            var shardsUsed = 0;
            //Go through all type sets
            Parallel.ForEach(Sets, parallelOptions,
                (IntegrationSet itemSet, ParallelLoopState itemSetState) =>
                {
                    //We shouldn't get any more items
                    if ((ShardLimit != 0 && shardsUsed >= ShardLimit) ||
                        (TotalEntryLimit != 0 && totalItemsUsed > TotalEntryLimit))
                    {
                        itemSetState.Break();
                        return;
                    }

                    var shards = itemSet.Source.Shards();
                    if (ShardLimit > 0)
                    {
                        var shardsLeft = ShardLimit - shardsUsed;
                        shards = shards.Take(shardsLeft);
                    }
                    //Interlocked.Add(ref shardsUsed, shards.Count());
                    Parallel.ForEach(shards, parallelOptions, (sourceShard, loopState, index) =>
                    {
                        //No more shards allowed
                        if ((ShardLimit != 0 && shardsUsed >= ShardLimit) ||
                            (TotalEntryLimit != 0 && totalItemsUsed >= TotalEntryLimit))
                        {
                            loopState.Break();
                            return;
                        }
#if DEBUG
                        var threadId = Thread.CurrentThread.ManagedThreadId;
                        Debug.WriteLine($"[T{threadId}]Harvester reading: {sourceShard}");
#endif
                        using (sourceShard)
                        {
                            if (sourceShard.SupportsSeeking)
                            {
                                var elements = sourceShard.AsEnumerable();
                                if (TotalEntryLimit != 0)
                                {
                                    var entriesLeft = TotalEntryLimit - totalItemsUsed;
                                    elements = elements.Take(entriesLeft);
                                }
                                Parallel.ForEach(elements, parallelOptions, (entry, itemLoopState, itemIndex) =>
                                {
                                    if (TotalEntryLimit != 0 && totalItemsUsed >= TotalEntryLimit)
                                    {
                                        itemSetState.Break();
                                        return;
                                    }
                                    var document = itemSet.Wrap(entry);
                                    Destination.Post(document);
                                    Interlocked.Increment(ref totalItemsUsed);
                                });
                            }
                            else
                            {
                                dynamic entry;
                                while ((entry = sourceShard.GetNext()) != null)
                                {
                                    if (TotalEntryLimit != 0 && totalItemsUsed >= TotalEntryLimit) break;
                                    IntegratedDocument document = itemSet.Wrap(entry);
                                    Destination.Post(document);
                                    Interlocked.Increment(ref totalItemsUsed);
                                }
                            }
                        }
                        Interlocked.Increment(ref shardsUsed);
                    });
                });
            totalShardsUsed = shardsUsed;
            return totalItemsUsed;
        }

        /// <summary>
        /// Gets the elapsed time for syncs
        /// </summary>
        /// <returns></returns>
        public TimeSpan ElapsedTime()
        {
            return _stopwatch.Elapsed;
        }

        /// <summary>
        /// Limit the total number of input sources that this harvester can consume.
        /// </summary>
        /// <param name="max"></param>
        public void LimitShards(int max)
        {
            _shardLimit = max;
        }
        /// <summary>
        /// Limit the toal number of input entries that this havester can consume.
        /// </summary>
        /// <param name="max"></param>
        public void LimitEntries(int max)
        {
            _totalEntryLimit = max;
        }


    }
}
