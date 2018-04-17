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

        [Theory]
        [InlineData(new object[] { "feature_test.csv", "DAY(first_feature_test.csv_time)", "DAY(first(dstime(feature_test.csv)))" })]
        [InlineData(new object[] { "feature_test.csv", "first(DAY(first_feature_test.csv_time))", "first(DAY(first(dstime(feature_test.csv))))" })]
        [InlineData(new object[] { "feature_test", "DAY(first_feature_test_time)", "DAY(first(dstime(feature_test)))" })]
        [InlineData(new object[] { "feature_test", "DAY(feature_test_time)", "DAY(dstime(feature_test))" })]
        public void ParseNonStandardExpressions(string integrationName, string code, string expected)
        {
            var integration = new DataIntegration(integrationName);
            integration.DataTimestampColumn = "timestamp";
            var tokenizer = new FeatureToolsTokenizer(integration);
            var dslTokens = tokenizer.Tokenize(code);
            var parser = new DonutSyntaxReader(dslTokens, integration);
            var exps = parser.ReadExpressions().FirstOrDefault();
            Assert.Equal(expected, exps.ToString());
        }

        //        [Fact]
        //        public void ParseDottedIntegrationExpression()
        //        {
        //            var code = @"DAY(first_Romanian_time)";
        //            var integration = new DataIntegration("Romanian");
        //            integration.DataTimestampColumn = "timestamp";
        //            var tokenizer = new FeatureToolsTokenizer(integration);
        //            var parser = new DonutSyntaxReader(tokenizer.Tokenize(code), integration);
        //            var exps = parser.ReadExpressions().FirstOrDefault();
        //            Assert.Equal("DAY(first(dstime(Romanian)))", exps.ToString());
        //        }

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
