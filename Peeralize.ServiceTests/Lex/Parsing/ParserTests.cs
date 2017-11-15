using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peeralize.Service.Lex.Expressions;
using Peeralize.Service.Lex.Parsing.Tokenizers;
using Xunit;

namespace Peeralize.ServiceTests.Lex.Parsing
{
    public class ParserTests
    {

        [Theory]
        [InlineData(new object[]{ "f(a)" })]
        public void TokenizeFunction1(string fc)
        {
            var tokenizer = new PrecedenceTokenizer();
            var tokens = tokenizer.Tokenize(fc).ToList();
            var parser = new Peeralize.Service.Lex.Parsing.Parser();
            parser.Load(tokens);
            var x = parser.ReadFunction();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count > 0);
            var parameterExpression = x.Parameters.First();
            Assert.IsType<VariableExpression>(parameterExpression.Value);
            Assert.Equal(((VariableExpression) parameterExpression.Value).Name, "a");
        }


        [Theory]
        [InlineData(new object[] { "f(a,b)" })]
        [InlineData(new object[] { "f(a,b,c)" })]
        public void TokenizeFunction2(string fc)
        {
            var tokenizer = new PrecedenceTokenizer();
            var tokens = tokenizer.Tokenize(fc).ToList();
            var parser = new Peeralize.Service.Lex.Parsing.Parser();
            parser.Load(tokens);
            var x = parser.ReadFunction();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count >= 2);
            var param1 = x.Parameters.First();
            var param2 = x.Parameters.Skip(1).First();
            Assert.IsType<VariableExpression>(param1.Value);
            Assert.IsType<VariableExpression>(param2.Value);
            Assert.Equal(((VariableExpression)param1.Value).Name, "a");
            Assert.Equal(((VariableExpression)param2.Value).Name, "b");
        }

        [Theory]
        [InlineData(new object[] { "f(a.one,b)" })] 
        public void TokenizeFunction3(string fc)
        {
            var tokenizer = new PrecedenceTokenizer();
            var tokens = tokenizer.Tokenize(fc).ToList();
            var parser = new Peeralize.Service.Lex.Parsing.Parser();
            parser.Load(tokens);
            var x = parser.ReadFunction();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count >= 2);
            var param1 = x.Parameters.First();
            var param2 = x.Parameters.Skip(1).First();
            Assert.IsType<VariableExpression>(param1.Value);
            Assert.IsType<VariableExpression>(param2.Value);
            Assert.Equal(((VariableExpression)param1.Value).Name, "a.one"); 
            Assert.Equal(((VariableExpression)param2.Value).Name, "b"); 
        }
    }
}
