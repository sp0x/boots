using System;
using System.Linq;
using nvoid.Integration;
using Netlyt.Service;
using Netlyt.Service.Build;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generation;
using Netlyt.Service.Lex.Generators;
using Netlyt.Service.Lex.Parsing;
using Netlyt.Service.Lex.Parsing.Tokenizers;
using Xunit;

namespace Netlyt.ServiceTests.Lex.Expressions
{
    [Collection("Entity Parsers")]
    public class DonutExpressionTests : IDisposable
    {
        private ApiService _apiService;
        private ApiAuth _appAuth;
        private IntegrationService _integrationService;

        public DonutExpressionTests(ConfigurationFixture fixture)
        {
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
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
            var compiler = new DonutCompiler(dscript);
            var assembly = compiler.Compile("someAssembly");
            var xtype = assembly.GetType("someAssembly.modelName");
            var types = assembly.GetExportedTypes();
            var xtypes = assembly.GetTypes();
            assembly = assembly;
            //Comple the context 
            //Assert.True(emittedBlob.Length > 100);
            //Generate the code for a map reduce with mongo
        }
        public void Dispose()
        {
        }
    }
}