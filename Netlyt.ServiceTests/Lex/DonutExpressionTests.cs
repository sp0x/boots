using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.Integration;
using Netlyt.Service;
using Netlyt.Service.Donut;
using Netlyt.Service.FeatureGeneration;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.IntegrationSource;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Parsing;
using Netlyt.Service.Lex.Parsing.Tokenizers;
using Netlyt.ServiceTests.Netinfo;
using Xunit;

namespace Netlyt.ServiceTests.Lex
{
    [Collection("Entity Parsers")]
    public class DonutExpressionTests : IDisposable
    {
        private ApiService _apiService;
        private ApiAuth _appAuth;
        private IntegrationService _integrationService;
        private CompilerService _compiler;
        private RedisCacher _cacher;
        private IServiceProvider _serviceProvider;

        public DonutExpressionTests(ConfigurationFixture fixture)
        {
            _compiler = fixture.GetService<CompilerService>();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _cacher = fixture.GetService<RedisCacher>(); 
            _serviceProvider = fixture.GetService<IServiceProvider>();

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
            var tokenizer = new PrecedenceTokenizer();
            var parser = new TokenParser(tokenizer.Tokenize(txt));
            DonutScript dscript = parser.ParseDonutScript();
            Assert.Equal("events", dscript.Integrations.FirstOrDefault());
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
            "
        })]
        public void GenerateDonutContext(string txt)
        {
            var tokenizer = new PrecedenceTokenizer();
            var parser = new TokenParser(tokenizer.Tokenize(txt));
            DonutScript dscript = parser.ParseDonutScript();   
            Type donutType;
            Type donutContextType;
            Type donutFGen;
            var assembly = _compiler.Compile(dscript, "someAssembly", out donutType, out donutContextType, out donutFGen);
            Assert.NotNull(donutType);
            Assert.NotNull(donutContextType);
            Assert.NotNull(donutFGen);
            var memberInfo = typeof(DonutContext);
            Assert.True(donutContextType.BaseType.Name==memberInfo.Name);
            var genericDonutfileRoot = typeof(Donutfile<>).MakeGenericType(donutContextType);
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
            ", "057cecc6-0c1b-44cd-adaa-e1089f10cae8_reduced"
        })]
        public async Task TestGeneratedDonutContext(string txt, string collectionName)
        {
            var tokenizer = new PrecedenceTokenizer();
            var parser = new TokenParser(tokenizer.Tokenize(txt));
            DonutScript dscript = parser.ParseDonutScript();
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, "someAssembly2", out donutType, out donutContextType, out donutFEmitterType); 
            //Source
            MongoSource<ExpandoObject> source = MongoSource.CreateFromCollection(collectionName, new BsonFormatter<ExpandoObject>());
            source.SetProjection(x =>
            {
                if (!((IDictionary<string,object>)x).ContainsKey("value")) ((dynamic)x).value.day = ((dynamic)x)._id.day;
                return ((dynamic)x).value as ExpandoObject;
            });
            source.ProgressInterval = 0.05; 
            var harvester = new Netlyt.Service.Harvester<IntegratedDocument>(_apiService, _integrationService, 10);
            var entryLimit = (uint)10000;
            harvester.LimitEntries(entryLimit);
            var integration = harvester.AddIntegrationSource(source, _appAuth, "SomeIntegrationName2"); 
            var donutMachine = DonutBuilderFactory.Create(donutType, donutContextType, integration, _cacher, _serviceProvider);
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(source.Size);
            donut.ReplayInputOnFeatures = true;

            IDonutRunner donutRunner = DonutRunnerFactory.Create(donutType, donutContextType, harvester); 
            var featureGenerator = FeatureGeneratorFactory.Create(donut, donutFEmitterType);
             
            var result = await donutRunner.Run(donut, featureGenerator);
            Assert.NotNull(result);
            Assert.Equal((int)entryLimit, (int)result.ProcessedEntries);
            Debug.WriteLine(result.ProcessedEntries); 

            //TODO: Add assembly unloading whenever coreclr implements it..
            //Comple the context 
            //Assert.True(emittedBlob.Length > 100);
            //Generate the code for a map reduce with mongo
        }

        public void Dispose()
        {
        }
    }
}