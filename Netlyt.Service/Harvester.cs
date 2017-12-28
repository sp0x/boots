using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using nvoid.db.DB;
using nvoid.db.Extensions;
using nvoid.exec.Blocks;
using Netlyt.Service.Integration; 
using Netlyt.Service.IntegrationSource; 

namespace Netlyt.Service
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
    public class Harvester<TDocument> : Entity
    {
        private Stopwatch _stopwatch;
        private int _shardLimit;
        private int _totalEntryLimit;

        public HashSet<IntegrationSet> Sets { get; private set; } 
        /// <summary>
        /// The destination to which documents will be dispatched
        /// </summary>
        public IFlowBlock<TDocument> Destination { get; private set; }
        public ITargetBlock<TDocument> DestinationBlock { get; private set; }

        protected int ShardLimit => _shardLimit;
        protected int TotalEntryLimit => _totalEntryLimit;
        public uint ThreadCount { get; private set; }

        public Harvester(uint threadCount = 4) : base()
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
        public Harvester<TDocument> AddType(IIntegrationTypeDefinition input, InputSource source)
        {
            if (input == null) throw new ArgumentException(nameof(input));
            var newSet = new IntegrationSet(input, source);
            Sets.Add(newSet);
            return this;
        }

        public IntegrationTypeDefinition AddPersistentType(string name, string appId, InputSource source)
        {

            IntegrationTypeDefinition type = IntegrationTypeDefinition.Named(appId, name);
            IntegrationTypeDefinition existingType = null;
            if (!IntegrationTypeDefinition.TypeExists(type, appId, out existingType)) type.Save();
            else type = existingType;
            this.AddType(type, source); 
            return type;
        }

        /// <summary>
        /// Resolves the type from the input, persists it to the DB, and adds it as an integration set from the given source.
        /// </summary>
        /// <param name="inputSource"></param>
        /// <param name="userId"></param>
        /// <param name="name">The name of the type that will be created</param>
        /// <param name="persist">Whether the type should be saved</param>
        /// <returns></returns>
        public IntegrationTypeDefinition AddPersistentType(InputSource inputSource, string userId, string name, bool persist = true, string outputCollection = null)
        {
            if (!(inputSource.GetTypeDefinition() is IntegrationTypeDefinition type))
            {
                throw new Exception("Could not resolve type!");
            }
            type.APIKey = userId;
            type.Collection = outputCollection;
            if (!string.IsNullOrEmpty(name))
            {
                type.Name = name;
            }
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
        public Harvester<TDocument> SetDestination(IFlowBlock<TDocument> dest)
        {
            Destination = dest;
            return this;
        }

        public Harvester<TDocument> SetDestination(ITargetBlock<TDocument> dest)
        {
            DestinationBlock = dest;
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
            //Destination.ConsumeAsync(cToken);
            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = (int)ThreadCount };
            parallelOptions.CancellationToken = cToken;
            ResetStopwatch();
            _stopwatch.Start();
            int shardsUsed;
            var totalItemsUsed = ProcessInputShards(parallelOptions, out shardsUsed);
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

        /// <summary>
        /// Reads all values from the source, in raw means without any pre-processing.
        /// </summary>
        /// <param name="target">A target block to which to post all input.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<HarvesterResult> ReadAll(
            ITargetBlock<ExpandoObject> target,
            CancellationToken? cancellationToken = null)
        {
            var cToken = cancellationToken ?? CancellationToken.None;
            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = (int)ThreadCount };
            parallelOptions.CancellationToken = cToken;
            ResetStopwatch();
            _stopwatch.Start();
            int shardsUsed;
            var totalItemsUsed = ProcessInputShardsRaw(target, parallelOptions, out shardsUsed);
            target.Complete();
            return target.Completion.ContinueWith(x =>
            {
                _stopwatch.Stop();
                var output = new HarvesterResult(shardsUsed, totalItemsUsed);
                return output;
            });
        }

        /// <summary>
        /// Reads the input raw, as an array of string arrays and posts it to the target.
        /// </summary>
        /// <param name="targetBlock"></param>
        /// <param name="parallelOptions"></param>
        /// <param name="totalShardsUsed"></param>
        /// <returns></returns>
        private int ProcessInputShardsRaw(ITargetBlock<ExpandoObject> targetBlock, ParallelOptions parallelOptions, out int totalShardsUsed)
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
                                    var sendTask = targetBlock.SendAsync(entry as ExpandoObject);
                                    sendTask.Wait(); 
                                    Interlocked.Increment(ref totalItemsUsed);
                                });
                            }
                            else
                            {
                                ExpandoObject entry;
                                while ((entry = sourceShard.GetNext<ExpandoObject>()) != null)
                                {
                                    if (TotalEntryLimit != 0 && totalItemsUsed >= TotalEntryLimit) break;
                                    targetBlock.SendChecked(entry,
                                        () => TotalEntryLimit != 0 && totalItemsUsed >= TotalEntryLimit); 
                                     
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
        /// 
        /// </summary>
        /// <param name="parallelOptions"></param>
        /// <param name="totalShardsUsed"></param>
        /// <returns></returns>
        private int ProcessInputShards(ParallelOptions parallelOptions, out int totalShardsUsed)
        {
            var totalItemsUsed = 0;
            var shardsUsed = 0;
            if (Sets.Count == 0)
            {
                throw new Exception("No sets to process!");
            }
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
                                    if (Destination == null)
                                    {
                                        nvoid.exec.Blocks.Extensions.SendChecked(DestinationBlock, document, null);
                                    }
                                    else
                                    {
                                        Destination.SendAsync(document).Wait();
                                    }
                                    
                                    Interlocked.Increment(ref totalItemsUsed);
                                });
                            }
                            else
                            {
                                dynamic entry;
                                while ((entry = sourceShard.GetNext()) != null)
                                {
                                    if (TotalEntryLimit != 0 && totalItemsUsed >= TotalEntryLimit) break;
                                    TDocument valueToSend;
                                    if (typeof(TDocument) == typeof(IntegratedDocument))
                                    {
                                        valueToSend = itemSet.Wrap(entry);
                                    }
                                    else
                                    {
                                        valueToSend = entry;
                                    } 
                                    Task<bool> sendTask = null;
                                    if (Destination == null)
                                    {
                                        sendTask = DataflowBlock.SendAsync<TDocument>(DestinationBlock, valueToSend);
                                    }
                                    else
                                    {
                                        sendTask = Destination.SendAsync(valueToSend);
                                    }
                                    var resultingTask = Task.WhenAny(Task.Delay(1000000000), sendTask).Result;
                                    if (sendTask != resultingTask)
                                    {
                                        throw new Exception($"Block timeout while emitting {totalItemsUsed}-th item!");
                                    }
                                    Interlocked.Increment(ref totalItemsUsed);
                                }
                            }
                        }
                        Interlocked.Increment(ref shardsUsed);
                    });
                    itemSet.Source.Cleanup();
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
