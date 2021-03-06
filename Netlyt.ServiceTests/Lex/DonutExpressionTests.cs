﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Donut;
using Donut.Caching;
using Donut.Data.Format;
using Donut.FeatureGeneration;
using Donut.IntegrationSource;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Donut.Lex.Parsing;
using Donut.Parsing.Tokenizers;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.ServiceTests.Fixtures;
using Xunit;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.ServiceTests.Lex
{
    [Collection("DonutTests")]
    public class DonutExpressionTests : IDisposable
    {
        private ApiService _apiService;
        private ApiAuth _appAuth;
        private IIntegrationService _integrationService;
        private CompilerService _compiler;
        private RedisCacher _cacher;
        private IServiceProvider _serviceProvider;
        private IDatabaseConfiguration _dbConfig;
        private ManagementDbContext _context;

        public DonutExpressionTests(DonutConfigurationFixture fixture)
        {
            _compiler = fixture.GetService<CompilerService>();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IIntegrationService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _cacher = fixture.GetService<RedisCacher>();
            _dbConfig = DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig();
            _serviceProvider = fixture.GetService<IServiceProvider>();
            _context = fixture.GetService<ManagementDbContext>();

        }
        /// <summary>
        /// </summary>
        /// <param name="txt"></param>
        [Theory]
        [InlineData(new object[]
        {
            @"define modelName
            from events
            set id = this.id
            set uuid = this.uuid
            "
        })]
        public void ParseSimpleDScript1(string txt)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(txt));
            DonutScript dscript = parser.ParseDonutScript();
            Assert.Equal("events", dscript.Integrations.FirstOrDefault().Name);
            AssignmentExpression f1 = dscript.Features[0];
            AssignmentExpression f2 = dscript.Features[1];
            Assert.Equal("id", f1.Member.Name);
            Assert.Equal("uuid", f2.Member.Name);
        }


        /// <summary>
        /// </summary>
        /// <param name="txt"></param> 
        [Theory]
        [InlineData(new object[]
        {
            @"define modelName
            from events
            set id = this.id
            set uuid = this.uuid
            target uuid
            "
        })]
        public void GenerateDonutContext(string txt)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(txt));
            DonutScript dscript = parser.ParseDonutScript();
            Type donutType;
            Type donutContextType;
            Type donutFGen;
            var assembly = _compiler.Compile(dscript, "someAssembly4", out donutType, out donutContextType, out donutFGen);
            Assert.NotNull(donutType);
            Assert.NotNull(donutContextType);
            Assert.NotNull(donutFGen);
            var memberInfo = typeof(DonutContext);
            Assert.True(donutContextType.BaseType.Name == memberInfo.Name);
            var genericDonutfileRoot = typeof(Donutfile<,>).MakeGenericType(donutContextType, typeof(IntegratedDocument));
            Assert.True(genericDonutfileRoot.IsAssignableFrom(donutType));
            var fgenDonutProperty = donutFGen.GetProperty("Donut");
            Assert.NotNull(fgenDonutProperty);
            Assert.True(fgenDonutProperty.PropertyType == donutType);
            //TODO: Add assembly unloading whenever coreclr implements it..
            //Comple the context 
            //Assert.True(emittedBlob.Length > 100);
            //Generate the code for a map reduce with mongo
        }

        /// <summary>
        /// </summary>
        /// <param name="txt"></param> 
        [Theory]
        [InlineData(new object[]
        {
            @"define modelName
            from events
            set id = this.id
            set uuid = this.uuid
            "
        })]
        public void DonutRecompilation(string txt)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(txt));
            DonutScript dscript = parser.ParseDonutScript();
            Type donutType;
            Type donutContextType;
            Type donutFGen;
            var asmName = "someAssembly5";
            var assembly = _compiler.Compile(dscript, asmName, out donutType, out donutContextType, out donutFGen);
            var memberInfo = typeof(DonutContext);
            var genericDonutfileRoot = typeof(Donutfile<,>).MakeGenericType(donutContextType, typeof(IntegratedDocument));
            var fgenDonutProperty = donutFGen.GetProperty("Donut");
            var asm2 = _compiler.Compile(dscript, asmName, out donutType, out donutContextType, out donutFGen);
        }

        [Theory]
        [InlineData("this.uuid", "057cecc6-0c1b-44cd-adaa-e1089f10cae8_reduced")]
        public async Task TestGeneratedDonutContextForFeature(string feature, string collectionName)
        {

            //Source
            MongoSource<ExpandoObject> source = MongoSource.CreateFromCollection(collectionName, new BsonFormatter<ExpandoObject>());
            source.SetProjection(x =>
            {
                if (!((IDictionary<string, object>)x).ContainsKey("value")) ((dynamic)x).value.day = ((dynamic)x)._id.day;
                return ((dynamic)x).value as ExpandoObject;
            });
            source.ProgressInterval = 0.05;
            var harvester = new Harvester<IntegratedDocument>(10);
            var entryLimit = (uint)10000;
            harvester.LimitEntries(entryLimit);
            var integration = DataIntegration.Wrap(harvester.AddIntegrationSource(source, _appAuth, "SomeIntegrationName3"));

            DonutScript dscript = DonutScript.Factory.CreateWithFeatures("SomeDonut1", null, integration, new[] { feature });
            dscript.AddIntegrations(integration);
            //parser.ParseDonutScript();
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, "someAssembly3", out donutType, out donutContextType, out donutFEmitterType);

            //Create a donut and a donutRunner
            var donutMachine = DonutGeneratorFactory.Create<IntegratedDocument>(donutType, donutContextType, integration, _cacher, _serviceProvider);
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(source.Size);
            donut.ReplayInputOnFeatures = true;
            IDonutRunner<IntegratedDocument> donutRunner = DonutRunnerFactory.CreateByType(donutType, donutContextType, harvester, _dbConfig, integration.FeaturesCollection);
            var featureGenerator = FeatureGeneratorFactory<IntegratedDocument>.Create(donut, donutFEmitterType);

            var result = await donutRunner.Run(donut, featureGenerator);
            var ftCol = new MongoList(_dbConfig.Name, integration.FeaturesCollection, _dbConfig.GetUrl());
            ftCol.Truncate();

            Assert.NotNull(result);
            Assert.Equal((int)entryLimit, (int)result.ProcessedEntries);
            Debug.WriteLine(result.ProcessedEntries);
        }
        /// <summary>
        /// </summary>
        /// <param name="script">THe donut script to execute</param>
        /// <param name="collectionName">The source collection</param> 
        [Theory]
        [InlineData(new object[]
        {
            @"define modelName
            from events
            set uuid = this.uuid
            ", "057cecc6-0c1b-44cd-adaa-e1089f10cae8_reduced"
        })]
        public async Task TestGeneratedDonutContext(string script, string collectionName)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(script));
            DonutScript dscript = parser.ParseDonutScript();
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, "someAssembly2", out donutType, out donutContextType, out donutFEmitterType);
            //Source
            MongoSource<ExpandoObject> source = MongoSource.CreateFromCollection(collectionName, new BsonFormatter<ExpandoObject>());
            source.SetProjection(x =>
            {
                if (!((IDictionary<string, object>)x).ContainsKey("value")) ((dynamic)x).value.day = ((dynamic)x)._id.day;
                return ((dynamic)x).value as ExpandoObject;
            });
            source.ProgressInterval = 0.05;
            var harvester = new Harvester<IntegratedDocument>(10);
            var entryLimit = (uint)10000;
            harvester.LimitEntries(entryLimit);
            var integration = harvester.AddIntegrationSource(source, _appAuth, "SomeIntegrationName2");

            //Create a donut and a donutRunner
            var donutMachine = DonutGeneratorFactory.Create<IntegratedDocument>(donutType, donutContextType, integration, _cacher, _serviceProvider);
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(source.Size);
            donut.ReplayInputOnFeatures = true;
            IDonutRunner<IntegratedDocument> donutRunner = DonutRunnerFactory.CreateByType(donutType, donutContextType, harvester, _dbConfig, integration.FeaturesCollection);
            var featureGenerator = FeatureGeneratorFactory<IntegratedDocument>.Create(donut, donutFEmitterType);

            var result = await donutRunner.Run(donut, featureGenerator);
            var ftCol = new MongoList(_dbConfig.Name, integration.FeaturesCollection, _dbConfig.GetUrl());
            ftCol.Truncate();

            Assert.NotNull(result);
            Assert.Equal((int)entryLimit, (int)result.ProcessedEntries);
            Debug.WriteLine(result.ProcessedEntries);
        }

        public void Dispose()
        {
        }
    }
}