using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Driver;
using nvoid.db.DB.MongoDB;
using Peeralize.Service;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Models;
using Peeralize.ServiceTests.Netinfo;
using Xunit;

namespace Peeralize.ServiceTests.Integration.Blocks
{
    [Collection("Entity Parsers")]
    public class IntegrationBlockTests
    {
        private static string AppId = "123123123";
        private Harvester<IntegratedDocument> GetHarvester(int threadCount = 20, int limit = 10)
        {
            var inputDirectory = Path.Combine(Environment
                .CurrentDirectory, "TestData\\Ebag\\1156");
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter()); 
            var harvester = new Peeralize.Service.Harvester<IntegratedDocument>(threadCount);
            harvester.LimitEntries(limit); 
            harvester.AddPersistentType(fileSource, AppId, null, false);
            return harvester;
        }
        [Fact]
        public async void TestSimple()
        {
            var harv = GetHarvester();
            var cnt = 0;
            var block = new StatsBlock(x =>
            {
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            harv.LimitEntries(100);
            harv.SetDestination(block);
            var results = await harv.Synchronize();
            Assert.Equal(results.ProcessedEntries, cnt);
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
            harv.LimitEntries(100);
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
            //Bug: this does not complete
            block.LinkTo(new IntegrationActionBlock("1234",  (act, x) =>
            {
                x["r"] = false;
                Interlocked.Increment(ref lCount);
            }));
            harv.LimitEntries(100);
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
                }, 4);
                block.LinkTo(action, null);
            }
            var iTransformOnCompletion = 0;
            var blockOps = new ExecutionDataflowBlockOptions() {MaxDegreeOfParallelism = 10};
            var blockA = new TransformBlock<IntegratedDocument, IntegratedDocument>(x =>
            {
                finishingTasks[Thread.CurrentThread.ManagedThreadId.ToString()] = "";
                Interlocked.Increment(ref iTransformOnCompletion);
                return x;
            }, blockOps);
            var blockB = new TransformBlock<IntegratedDocument, IntegratedDocument>(x =>
            {
                finishingTasks[Thread.CurrentThread.ManagedThreadId.ToString()] = "";
                Interlocked.Increment(ref iTransformOnCompletion);
                return x;
            }, blockOps);
            blockA.LinkTo(blockB);
            block.LinkOnComplete(blockA);
            //block.AddFlowCompletionTask(blockB.Completion);
            harv.SetDestination(block);
            var results = await harv.Synchronize();
            Assert.Equal(results.ProcessedEntries, iTransformOnCompletion / 2);
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
                block.LinkTo(action, null);
            }
            var iOnDone = 0;
            var iProcessDone = 0;
            var onDoneBlock = new IntegrationActionBlock("1234", (actionblock, x) =>
            {
                finishingTasks[Thread.CurrentThread.ManagedThreadId.ToString()] = "";
                Interlocked.Increment(ref iOnDone); 
            }, 10);
            onDoneBlock.LinkTo(new ActionBlock<IntegratedDocument>(x =>
            {
                Interlocked.Increment(ref iProcessDone);
            }));
            block.LinkOnComplete(onDoneBlock);
            harv.SetDestination(block);
            var results = await harv.Synchronize();
            Assert.Equal(results.ProcessedEntries, iOnDone);
            Assert.Equal(results.ProcessedEntries, iProcessDone);
            Assert.Equal(results.ProcessedEntries, cnt);
            Assert.Equal(results.ProcessedEntries * rCycles, lCount);
            Debug.WriteLine($"Threads used: after harvest {setx.Count} - finishing {finishingTasks.Count}");
            Debug.WriteLine($"Documents processed: " + docset.Count);
        }

        [Fact]
        public async void TestBranchedBlocks()
        {
            var harvester = GetHarvester(20);
            harvester.LimitEntries(1000);
            int cntFeatures=0, cntUpdates=0, cntBatchesApplied=0;
            int batchSize = 3000;
            var featureSize = 1;

            var grouper = new GroupingBlock(AppId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.Day}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                (doc, docx) => doc);
            // create features for each user -> create Update -> batch update
            var featureGenerator = new FeatureGenerator<IntegratedDocument>((doc) =>
            {
                Interlocked.Increment(ref cntFeatures);
                return new[]{ new KeyValuePair<string, object>("feature1", 0) };
            } , 8);
            var updateCreator = new TransformBlock<FeaturesWrapper<IntegratedDocument>, FindAndModifyArgs<IntegratedDocument>>((docFeatures) =>
            {
                Interlocked.Increment(ref cntUpdates);
                return new FindAndModifyArgs<IntegratedDocument>()
                {
                    Query = Builders<IntegratedDocument>.Filter.And(
                                Builders<IntegratedDocument>.Filter.Eq("Document.uuid", docFeatures.Document["uuid"].ToString()),
                                Builders<IntegratedDocument>.Filter.Eq("Document.noticed_date", docFeatures.Document.GetDate("noticed_date"))),
                    Update = docFeatures.Features.ToMongoUpdate<IntegratedDocument, object>()
                };
            }); 
            var updateBatcher = BatchedBlockingBlock< FindAndModifyArgs < IntegratedDocument > >.CreateBlock(batchSize);
            var updateApplier = new ActionBlock<FindAndModifyArgs<IntegratedDocument>[]>(x =>
            {
                Interlocked.Increment(ref cntBatchesApplied);
            });
            IPropagatorBlock<IntegratedDocument, FeaturesWrapper<IntegratedDocument>> featuresBlock = featureGenerator.CreateFeaturesBlock();
            featuresBlock.LinkTo(updateCreator, new DataflowLinkOptions() { PropagateCompletion = true });
            updateCreator.LinkTo(updateBatcher, new DataflowLinkOptions() { PropagateCompletion = true });
            updateBatcher.LinkTo(updateApplier, new DataflowLinkOptions() { PropagateCompletion = true });
            grouper.AddFlowCompletionTask(updateApplier.Completion);
            grouper.LinkOnCompleteEx(featuresBlock);
            
            //grouper.OnProcessingCompletion(()=>featuresBlock.Complete());
//            grouper.ContinueWith((Task x) =>
//            {
//                featuresBlock.Complete();
//            });
            harvester.SetDestination(grouper);
            var syncTask = harvester.Synchronize();
            HarvesterResult results = await syncTask;
            Debug.WriteLine($"Updates created {cntUpdates}");
            Assert.True(cntBatchesApplied!=0 && (cntBatchesApplied * batchSize >= results.ProcessedEntries));
            Assert.True(cntUpdates > 0);
            Assert.True(cntUpdates == (cntFeatures * featureSize));
            
        }
         
    }
}
