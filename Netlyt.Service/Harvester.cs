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
using nvoid.Integration;
using Netlyt.Service.Data;
using Netlyt.Service.Integration; 
using Netlyt.Service.IntegrationSource; 
using IntegrationFactory = Netlyt.Service.Integration.DataIntegration.Factory;

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
        private uint _shardLimit;
        private uint _totalEntryLimit;
        private ApiService _apiService;
        private IntegrationService _integrationService;

        public HashSet<IntegrationSet> IntegrationSets { get; private set; } 
        /// <summary>
        /// The destination to which documents will be dispatched
        /// </summary>
        public IFlowBlock<TDocument> Destination { get; private set; }
        public ITargetBlock<TDocument> DestinationBlock { get; private set; }

        protected uint ShardLimit => _shardLimit;
        protected uint TotalEntryLimit => _totalEntryLimit;
        public uint ThreadCount { get; private set; }

        public Harvester(ApiService apiService,
            IntegrationService integrationService,
            uint threadCount = 4) : base()
        {
            IntegrationSets = new HashSet<IntegrationSet>();
            this.ThreadCount = threadCount;
            _stopwatch = new Stopwatch();
            _apiService = apiService;
            _integrationService = integrationService;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input">Details about the type</param>
        /// <param name="source">The source from which to pull the input</param>
        public Harvester<TDocument> AddType(IIntegration input, InputSource source)
        {
            if (input == null) throw new ArgumentException(nameof(input));
            var newSet = new IntegrationSet(input, source);
            IntegrationSets.Add(newSet);
            return this;
        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="name">The name of the integration</param>
//        /// <param name="appId">AppId key</param>
//        /// <param name="source">The input source</param>
//        /// <returns></returns>
//        public DataIntegration AddIntegrationSource(string name, string appId, InputSource source)
//        {
//
//            DataIntegration type = Integration.DataIntegration.Factory.CreateNamed(appId, name);
//            _integrationService.SaveOrFetchExisting(ref type);  
//            this.AddType(type, source); 
//            return type;
//        }

        /// <summary>
        /// Resolves the integration type from the input, persists it to the DB, and adds it as an integration set from the given source.
        /// </summary>
        /// <param name="inputSource">The input to use for reading. It's registered to the api auth.</param>
        /// <param name="appAuth">The api auth to which to link the integration</param>
        /// <param name="name">The name of the type that will be created</param>
        /// <param name="persist">Whether the type should be saved</param>
        /// <returns></returns>
        public DataIntegration AddIntegrationSource(
            InputSource inputSource,
            ApiAuth appAuth,
            string name,
            bool persist = true,
            string outputCollection = null)
        {
            var integration = inputSource.ResolveIntegrationDefinition() as DataIntegration;
            if (integration==null)
            {
                throw new Exception("Could not resolve type!");
            }
            if (integration.Fields.Count == 0)
            {
                throw new InvalidOperationException("Integration needs to have at least 1 field.");
            }
            integration.APIKey = appAuth;
            integration.Collection = outputCollection;
            if (!string.IsNullOrEmpty(name))
            {
                integration.Name = name;
            }
            if (persist)
            { 
                _integrationService.SaveOrFetchExisting(ref integration); 
            }
            AddType(integration, inputSource);
            return integration;
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
        public Task<HarvesterResult> Run(CancellationToken? cancellationToken = null)
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
            Parallel.ForEach(IntegrationSets, parallelOptions,
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
                        shards = shards.Take((int)shardsLeft);
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
                                    elements = elements.Take((int)entriesLeft);
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
                                //ExpandoObject entry; 
                                var iterator = sourceShard.GetIterator<ExpandoObject>();
                                foreach (var entry in iterator)
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
        /// Goes through all the integrations.
        /// </summary>
        /// <param name="parallelOptions"></param>
        /// <param name="totalShardsUsed"></param>
        /// <returns></returns>
        private int ProcessInputShards(ParallelOptions parallelOptions, out int totalShardsUsed)
        {
            var totalItemsUsed = 0;
            var shardsUsed = 0;
            if (IntegrationSets.Count == 0)
            {
                throw new Exception("No sets to process!");
            }
            //Go through all type sets
            Parallel.ForEach(IntegrationSets, parallelOptions,
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
                        shards = shards.Take((int)shardsLeft);
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
                                    elements = elements.Take((int)entriesLeft);
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
                                //dynamic entry;
                                var iterator = sourceShard.GetIterator();
                                foreach (var entry in iterator)
                                {
                                    if (entry == null) break;
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
                                    var resultingTask = sendTask.Result;
//                                    var resultingTask = Task.WhenAny(Task.Delay(1000000000), sendTask).Result;
//                                    if (sendTask != resultingTask)
//                                    {
//                                        throw new Exception($"Block timeout while emitting {totalItemsUsed}-th item!");
//                                    }
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
        public void LimitShards(uint max)
        {
            _shardLimit = max;
        }
        /// <summary>
        /// Limit the toal number of input entries that this havester can consume.
        /// </summary>
        /// <param name="max"></param>
        public void LimitEntries(uint max)
        {
            _totalEntryLimit = max;
        }


    }
}
