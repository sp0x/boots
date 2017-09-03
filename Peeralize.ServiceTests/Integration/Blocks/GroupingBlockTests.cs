﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using Peeralize.Service.Format;
using Peeralize.Service.Integration;
using Peeralize.Service.Integration.Blocks;
using Peeralize.Service.IntegrationSource;
using Xunit;

namespace Peeralize.ServiceTests.Integration.Blocks
{
    [Collection("Entity Parsers")]
    public class GroupingBlockTests
    {
        private GroupingBlock GetGrouper(string userId)
        {
            var grouper = new GroupingBlock(userId,
                (document) => $"{document.GetString("uuid")}_{document.GetDate("ondate")?.Day}",
                (document) => document.Define("noticed_date", document.GetDate("ondate")).RemoveAll("event_id", "ondate", "value", "type"),
                (accumulated, document) =>
                {
                    accumulated.AddDocumentArrayItem("events", new
                    {
                        ondate = document["ondate"].ToString(),
                        event_id = document.GetInt("event_id"),
                        type = document.GetInt("type"),
                        value = document["value"]?.ToString()
                    }.ToBsonDocument());
                    return null;
                });
            return grouper;
        }


        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void TestSimpleGroup(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment
                .CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter()); 
            var userId = "123123123"; 
            var harvester = new Peeralize.Service.Harvester(20);
            var grouper = GetGrouper(userId);
            harvester.LimitEntries(10);
            harvester.SetDestination(grouper); 
            harvester.AddPersistentType(fileSource, userId);
            var results = await harvester.Synchronize();
            Assert.True(results.ProcessedEntries == 10 && grouper.EntityDictionary.Count > 0);
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
        }

        [Theory]
        [InlineData(new object[] { "TestData\\Ebag\\1156" })]
        public async void TestTransformationGroup(string inputDirectory)
        {
            inputDirectory = Path.Combine(Environment
                .CurrentDirectory, inputDirectory);
            var fileSource = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter());
            var userId = "123123123";
            var harvester = new Peeralize.Service.Harvester(20);

            var grouper = GetGrouper(userId);
            var statsCounter = 0;
            var statsBlock = new StatsBlock((visit) =>
            {
                visit = visit;
                Interlocked.Increment(ref statsCounter);
                Thread.Sleep(1000);
            }); 
            grouper.BroadcastTo(statsBlock, null);
            harvester.LimitEntries(10);
            harvester.SetDestination(grouper);
            harvester.AddPersistentType(fileSource, userId);
            var results = await harvester.Synchronize();
            //Assert.True(results.ProcessedEntries == 10 && grouper.EntityDictionary.Count > 0);
            //Ensure that we went through all the items, with our entire dataflow.
            Assert.True(results.ProcessedEntries == statsCounter);
            var syncDuration = harvester.ElapsedTime();
            Debug.WriteLine($"Read all files in: {syncDuration.TotalSeconds}:{syncDuration.Milliseconds}");
        }
    }
}
