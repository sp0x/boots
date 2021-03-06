﻿using System;
using System.Dynamic;
using System.IO;
using Donut;
using Donut.Data;
using Donut.Data.Format;
using Donut.IntegrationSource;
using Donut.Lex;
using Donut.Lex.Expressions;
using Donut.Lex.Generation;
using Donut.Lex.Parsing;
using Donut.Parsing.Tokenizers;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.Lex.Expressions
{
    [Collection("Entity Parsers")]
    public class MapReduceExpressionTests : IDisposable
    {
        private ApiService _apiService;
        private ApiAuth _appId;
        private IIntegrationService _integrationService;

        public MapReduceExpressionTests(ConfigurationFixture fixture)
        {
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IIntegrationService>();
            _appId = _apiService.Generate();
            _apiService.Register(_appId);
        }

        /// <summary>
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="expectedFeatureTypeName"></param>
        /// <param name="expectedSource"></param>
        /// <param name="expectedPreSort"></param>
        /// <param name="expectedFeatureKVP"></param>
        [Theory]
        [InlineData(new object[]
        {
            @" 
            reduce day = time(this.ondate) / (60*60*24), 
                   uuid = this.uuid
            reduce_map  ondate = this.ondate,
                        value = this.value,
                        type = this.type
                    ",
            "day = time(this.ondate) / 60 * 60 * 24, uuid = this.uuid",
            "ondate = this.ondate, value = this.value, type = this.type",
           })]
        public void ParseMapReduceMap(
            string txt,
            string expectedKeys,
            string expectedValues
           )
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(txt));
            var mapReduce = parser.ReadMapReduce();
            var values = mapReduce.ValueMembers.ConcatExpressions();
            var keys = mapReduce.Keys.ConcatExpressions();
            Assert.Equal(expectedKeys, keys);
            Assert.Equal(expectedValues, values);
            var codeGen = mapReduce.GetCodeGenerator();
            var emittedBlob = codeGen.GenerateFromExpression(mapReduce);
            Assert.True(emittedBlob.Length > 100);
            //Generate the code for a map reduce with mongo
        }
        [Theory]
        [InlineData(new object[]
        {
            @"reduce day = time(this.ondate) / (60*60*24), 
                uuid = this.uuid
                reduce_map  ondate = this.ondate,
                value = this.value,
                type = this.type
            reduce aggregate 
                events = selectMany(values, (x) => x.events),
                uuid = key.uuid,
                day = key.day,
                noticed_date = if(any(events), events[0].ondate, null)"
        })]
        public void ParseMapReduceAggregate(string code)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(code));
            var mapReduce = parser.ReadMapReduce();
            var values = mapReduce.ValueMembers.ConcatExpressions();
            var keys = mapReduce.Keys.ConcatExpressions();
            var script = MapReduceJsScript.Create(mapReduce);
            Assert.True(script.Map.Length == 331);
            Assert.True(script.Reduce.Length == 805);
        }


        [Theory]
        [InlineData(new object[]
       {
            "TestData\\Ebag\\NewJoin",
           @"reduce day = time(this.ondate) / (60*60*24), 
                uuid = this.uuid
                reduce_map  ondate = this.ondate,
                value = this.value,
                type = this.type
            reduce aggregate
                events = selectMany(values, (x) => x.events),
                uuid = key.uuid,
                day = key.day,
                noticed_date = if(any(events), events[0].ondate, null)"
       })]
        public async void ExecuteMapReduce(string inputDirectory, string mapReduceDonut)
        {
            var currentDir = Environment.CurrentDirectory;
            inputDirectory = Path.Combine(currentDir, inputDirectory);
            Console.WriteLine($"Parsing data in: {inputDirectory}");
            uint entryLimit = 100;
            var importTask = new DataImportTask<ExpandoObject>(new DataImportTaskOptions
            {
                Source = FileSource.CreateFromDirectory(inputDirectory, new CsvFormatter<ExpandoObject>() { Delimiter = ';' }),
                ApiKey = _appId,
                IntegrationName = "TestingType",
                ThreadCount = 1, //So that we actually get predictable results with our limit!
                TotalEntryLimit = entryLimit
            }.AddIndex("ondate"));
            var importResult = await importTask.Import();
            await importTask.Reduce(mapReduceDonut, entryLimit, Builders<BsonDocument>.Sort.Ascending("ondate"));
            var dbc = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            var reducedCollection = new MongoList(dbc.Name, importTask.OutputDestinationCollection.ReducedOutputCollection, dbc.GetUrl());
            var reducedDocsCount = reducedCollection.Size;
            Assert.Equal(64, reducedDocsCount);
            //Cleanup
            importResult.Collection.Drop();
            reducedCollection.Trash();
        }

        public void Dispose()
        {
            _apiService.RemoveKey(_appId);
        }
    }
}
