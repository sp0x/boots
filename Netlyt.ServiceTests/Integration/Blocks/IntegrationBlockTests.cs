﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Donut;
using Donut.Blocks;
using Donut.Data.Format;
using Donut.FeatureGeneration;
using Donut.Features;
using Donut.IntegrationSource;
using MongoDB.Driver;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Blocks;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.Integration.Blocks
{
    [Collection("Entity Parsers")]
    public class IntegrationBlockTests
    { 

        private DynamicContextFactory _contextFactory;
        private ApiService _apiService;
        private ConfigurationFixture _config;
        private IntegrationService _integrationService;
        private ApiAuth _apiAuth;

        public IntegrationBlockTests(ConfigurationFixture fixture)
        {
            _config = fixture;
            _contextFactory = new DynamicContextFactory(() => _config.CreateContext());
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _apiAuth = _apiService.Generate();
            _apiService.Register(_apiAuth);
        }

        private Harvester<IntegratedDocument> GetHarvester(uint threadCount = 20, int limit = 10)
        {
            var inputDirectory = Path.Combine(Environment
                .CurrentDirectory, "TestData\\Ebag\\1156");
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>()); 
            var harvester = new Harvester<IntegratedDocument>(threadCount); 
            harvester.LimitEntries((uint)limit); 
            harvester.AddIntegrationSource(fileSource, _apiAuth, null);
            return harvester;
        }

        [Fact]
        public async void TestContinueWithChain()
        {
            var harv = GetHarvester();
            var cnt = 0;
            var str = "";
            var metaBlock = new MemberVisitingBlock<IntegratedDocument>(new Action<IIntegratedDocument>(x =>
            { 
            }));
            //Simple increment, because the continuations should run sequentially, not in parallel.
            metaBlock.ContinueWith(() =>
                {
                    str += "lo";
                    cnt++;
                })
                .ContinueWith(() => {
                    str += "l";
                    cnt++;
                });
            harv.SetDestination(metaBlock);
            harv.LimitEntries(100);
            var results = await harv.Run();
            Assert.Equal(2, cnt); 
            Assert.Equal("lol", str); 
        }

        [Fact]
        public async void TestSimple()
        {
            var harv = GetHarvester();
            var cnt = 0;
            var block = new StatsBlock<IntegratedDocument>(x =>
            {
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            harv.LimitEntries(100);
            harv.SetDestination(block);
            var results = await harv.Run();
            Assert.Equal(results.ProcessedEntries, cnt);
        }

        [Fact]
        public async void TestSingleLambda()
        {
            var harv = GetHarvester();
            var cnt = 0;
            var lCnt = 0;
            var block = new StatsBlock<IntegratedDocument>(x =>
            {
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            block.BroadcastTo(new ActionBlock<IIntegratedDocument>(x =>
            {
                x["1241"] = true;
                Interlocked.Increment(ref lCnt);
            }));
            harv.LimitEntries(100);
            harv.SetDestination(block);
            
            var results = await harv.Run();
            Assert.Equal(results.ProcessedEntries, cnt);
            Assert.Equal(results.ProcessedEntries, lCnt);
        }

        [Fact]
        public async void TestSingleBlock()
        {
            var harv = GetHarvester();
            var cnt = 0;
            var lCount = 0;
            var block = new StatsBlock<IntegratedDocument>(x =>
            { 
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            //Bug: this does not complete
            block.LinkTo(new IntegrationActionBlock<IntegratedDocument>("1234",  (act, x) =>
            {
                x["r"] = false;
                Interlocked.Increment(ref lCount);
            }));
            harv.LimitEntries(100);
            harv.SetDestination(block);
            var results = await harv.Run();
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
            var block = new StatsBlock<IntegratedDocument>(x =>
            {
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            var setx = new ConcurrentDictionary<string, string>();
            var finishingTasks = new ConcurrentDictionary<string, string>();
            var docset = new ConcurrentDictionary<string, string>();

            for (var i = 0; i < rCycles; i++)
            {
                var action = new IntegrationActionBlock<IntegratedDocument>("1234", (act, x) =>
                {
                    x["r"] = false;
                    //Debug.WriteLine($"Proc from block {act.Id}[{act.ThreadId}]: {x.Id}");
                    setx[act.ThreadId.ToString()] = "";
                    docset[x.Id.ToString()] = "";
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
            var results = await harv.Run();
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
            var block = new StatsBlock<IntegratedDocument>(x =>
            {
                x["lol"] = true;
                Interlocked.Increment(ref cnt);
            });
            var setx = new ConcurrentDictionary<string, string>();
            var finishingTasks = new ConcurrentDictionary<string, string>();
            var docset = new ConcurrentDictionary<string, string>();

            for (var i = 0; i < rCycles; i++)
            {
                var action = new IntegrationActionBlock<IntegratedDocument>("1234", (act, x) =>
                {
                    x["r"] = false;
                    Debug.WriteLine($"Proc from block {act.Id}[{act.ThreadId}]: {x.Id}");
                    setx[act.ThreadId.ToString()] = "";
                    docset[x.Id.ToString()] = "";
                    Interlocked.Increment(ref lCount);
                });
                block.LinkTo(action, null);
            }
            var iOnDone = 0;
            var iProcessDone = 0;
            var onDoneBlock = new IntegrationActionBlock<IntegratedDocument>("1234", (actionblock, x) =>
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
            var results = await harv.Run();
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
            var batchSize = (uint)3000;
            var featureSize = 1;

            var grouper = new GroupingBlock<IntegratedDocument>(_apiAuth,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.Day}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                (doc, docx) => doc);
            // create features for each user -> create Update -> batch update
            var featureGenerator = new FeatureGenerator<IIntegratedDocument>((doc) =>
            {
                Interlocked.Increment(ref cntFeatures);
                return new[]{ new KeyValuePair<string, object>("feature1", 0) };
            } , 8);
            var updateCreator = new TransformBlock<FeaturesWrapper<IIntegratedDocument>, FindAndModifyArgs<IIntegratedDocument>>((docFeatures) =>
            {
                Interlocked.Increment(ref cntUpdates);
                return new FindAndModifyArgs<IIntegratedDocument>()
                {
                    Query = Builders<IIntegratedDocument>.Filter.And(
                                Builders<IIntegratedDocument>.Filter.Eq("Document.uuid", docFeatures.Document["uuid"].ToString()),
                                Builders<IIntegratedDocument>.Filter.Eq("Document.noticed_date", docFeatures.Document.GetDate("noticed_date"))),
                    Update = docFeatures.Features.ToMongoUpdate<IIntegratedDocument, object>()
                };
            }); 
            var updateBatcher = BatchedBlockingBlock< FindAndModifyArgs < IIntegratedDocument > >.CreateBlock(batchSize);
            var updateApplier = new ActionBlock<FindAndModifyArgs<IIntegratedDocument>[]>(x =>
            {
                Interlocked.Increment(ref cntBatchesApplied);
            });
            IPropagatorBlock<IIntegratedDocument, FeaturesWrapper<IIntegratedDocument>> featuresBlock = featureGenerator.CreateFeaturesBlock();
            featuresBlock.LinkTo(updateCreator, new DataflowLinkOptions() { PropagateCompletion = true });
            updateCreator.LinkTo(updateBatcher, new DataflowLinkOptions() { PropagateCompletion = true });
            updateBatcher.LinkTo(updateApplier, new DataflowLinkOptions() { PropagateCompletion = true });
            grouper.AddCompletionTask(updateApplier.Completion);
            grouper.LinkOnCompleteEx(featuresBlock);
            
            //grouper.OnProcessingCompletion(()=>featuresBlock.Complete());
//            grouper.ContinueWith((Task x) =>
//            {
//                featuresBlock.Complete();
//            });
            harvester.SetDestination(grouper);
            var syncTask = harvester.Run();
            HarvesterResult results = await syncTask;
            Debug.WriteLine($"Updates created {cntUpdates}");
            Assert.True(cntBatchesApplied!=0 && (cntBatchesApplied * batchSize >= results.ProcessedEntries));
            Assert.True(cntUpdates > 0);
            Assert.True(cntUpdates == (cntFeatures * featureSize));
            
        }
         
    }
}
