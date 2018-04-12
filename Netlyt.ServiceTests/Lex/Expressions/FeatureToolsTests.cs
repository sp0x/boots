using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Service;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Parsing;
using Netlyt.Service.Lex.Parsing.Tokenizers;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.Lex.Expressions
{
    [Collection("Entity Parsers")]
    public class FeatureToolsTests
    {
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private ApiAuth _appId;

        public FeatureToolsTests(ConfigurationFixture fixture)
        {
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _appId = _apiService.Generate();
            _apiService.Register(_appId);
        }

        [Fact]
        public void ParseUnderscoresExpression()
        {
            var code = @"DAY(first_Romanian_time)";
            var integration = new DataIntegration("Romanian");
            integration.DataTimestampColumn = "timestamp";
            var tokenizer = new FeatureToolsTokenizer(integration);
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(code), integration);
            var exps = parser.ReadExpressions().FirstOrDefault();
            Assert.Equal("DAY(first(dstime(Romanian)))", exps.ToString());
        }

        [Fact]
        public void ParseProjectionExpression()
        {
            var code = @"Romanian_time";
            var integration = new DataIntegration("Romanian");
            integration.DataTimestampColumn = "timestamp";
            var tokenizer = new FeatureToolsTokenizer(integration);
            var dslTokens = tokenizer.Tokenize(code).ToList();
            var parser = new DonutSyntaxReader(dslTokens, integration);
            var exp = parser.ReadExpressions().FirstOrDefault();
            Assert.Equal("dstime(Romanian)", exp.ToString());
        }

        [Fact]
        public void ParseUnderscoreProjectionExpression()
        {
            var code = @"first_Romanian_time";
            var integration = new DataIntegration("Romanian");
            integration.DataTimestampColumn = "timestamp";
            var tokenizer = new FeatureToolsTokenizer(integration);
            var dslTokens = tokenizer.Tokenize(code).ToList();
            var parser = new DonutSyntaxReader(dslTokens, integration);
            var exp = parser.ReadExpressions().FirstOrDefault();
            Assert.Equal("first(dstime(Romanian))", exp.ToString());
        }
    }
}
