using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource;
using Xunit;

namespace Peeralize.ServiceTests.Integration.Blocks
{
    [Collection("Entity Parsers")]
    public class IntegrationBlockTests
    {
        private Harvester GetHarvester(int threadCount = 20, int limit = 10)
        {
            var inputDirectory = Path.Combine(Environment
                .CurrentDirectory, "TestData\\Ebag\\1156");
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());
            var userId = "123123123";
            var harvester = new Peeralize.Service.Harvester(threadCount);
            harvester.LimitEntries(limit); 
            harvester.AddPersistentType(fileSource, userId, false);
            return harvester;
        }


        [Fact]
        public async void TestSingleLambda()
        {
            var harv = GetHarvester();
            var cnt = 0;
            var lCnt = 0;
            var block = new StatsBlock(x =>
            {
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            block.BroadcastTo(new ActionBlock<IntegratedDocument>(x =>
            {
                x["1241"] = true;
                Interlocked.Increment(ref lCnt);
            }));
            harv.SetDestination(block);
            var results = await harv.Synchronize();
            Assert.Equal(results.ProcessedEntries, cnt);
            Assert.Equal(results.ProcessedEntries, lCnt);
        }

        [Fact]
        public async void TestSingleBlock()
        {
            var harv = GetHarvester();
            var cnt = 0;
            var lCount = 0;
            var block = new StatsBlock(x =>
            { 
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            block.BroadcastTo(new IntegrationActionBlock("1234",  (act, x) =>
            {
                x["r"] = false;
                Interlocked.Increment(ref lCount);
            }), null);
            harv.SetDestination(block);
            var results = await harv.Synchronize();
            Assert.Equal(results.ProcessedEntries, cnt);
            Assert.Equal(results.ProcessedEntries, lCount);
        } 

        [Fact]
        public async void TestMultipleLambdas()
        {
            var harv = GetHarvester(20, 100);
            var cnt = 0;
            var lCount = 0;
            var r = new Random();
            var rCycles = r.Next(1, 5);
            var block = new StatsBlock(x =>
            {
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            var setx = new ConcurrentDictionary<string, string>();
            var finishingTasks = new ConcurrentDictionary<string, string>();
            var docset = new ConcurrentDictionary<string, string>();

            for (var i = 0; i < rCycles; i++)
            {
                var action = new IntegrationActionBlock("1234", (act, x) =>
                {
                    x["r"] = false;
                    //Debug.WriteLine($"Proc from block {act.Id}[{act.ThreadId}]: {x.Id.Value}");
                    setx[act.ThreadId.ToString()] = "";
                    docset[x.Id.Value.ToString()] = "";
                    Interlocked.Increment(ref lCount);
                });
                block.BroadcastTo(action, null);
            }
            var iTransformOnCompletion = 0;
            block.LinkToCompletion(new TransformBlock<IntegratedDocument, IntegratedDocument>(x =>
            {
                finishingTasks[Thread.CurrentThread.ManagedThreadId.ToString()] = "";
                Interlocked.Increment(ref iTransformOnCompletion);
                return x;
            }, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 10 }));
            harv.SetDestination(block);
            var results = await harv.Synchronize();
            Assert.Equal(results.ProcessedEntries, iTransformOnCompletion);
            Assert.Equal(results.ProcessedEntries, cnt);
            Assert.Equal(results.ProcessedEntries * rCycles, lCount);
            Debug.WriteLine($"Threads used: after harvest {setx.Count} - finishing {finishingTasks.Count}");
            Debug.WriteLine($"Documents processed: " + docset.Count);
        }

        [Fact]
        public async void TestMultipleBlocks()
        {
            var harv = GetHarvester(20, 10);
            var cnt = 0;
            var lCount = 0;
            var r = new Random();
            var rCycles = r.Next(1, 5);
            var block = new StatsBlock(x =>
            {
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            var setx = new ConcurrentDictionary<string, string>();
            var finishingTasks = new ConcurrentDictionary<string, string>();
            var docset = new ConcurrentDictionary<string, string>();

            for (var i = 0; i < rCycles; i++)
            {
                var action = new IntegrationActionBlock("1234", (act, x) =>
                {
                    x["r"] = false;
                    Debug.WriteLine($"Proc from block {act.Id}[{act.ThreadId}]: {x.Id.Value}");
                    setx[act.ThreadId.ToString()] = "";
                    docset[x.Id.Value.ToString()] = "";
                    Interlocked.Increment(ref lCount);
                });
                block.BroadcastTo(action, null);
            }
            var iTransformOnCompletion = 0;
            block.LinkToCompletion(new IntegrationActionBlock("1234", (actionblock, x) =>
            {
                finishingTasks[Thread.CurrentThread.ManagedThreadId.ToString()] = "";
                Interlocked.Increment(ref iTransformOnCompletion); 
            }, 10));
            harv.SetDestination(block);
            var results = await harv.Synchronize();
            Assert.Equal(results.ProcessedEntries, iTransformOnCompletion);
            Assert.Equal(results.ProcessedEntries, cnt);
            Assert.Equal(results.ProcessedEntries * rCycles, lCount);
            Debug.WriteLine($"Threads used: after harvest {setx.Count} - finishing {finishingTasks.Count}");
            Debug.WriteLine($"Documents processed: " + docset.Count);
        } 
         
    }
}
