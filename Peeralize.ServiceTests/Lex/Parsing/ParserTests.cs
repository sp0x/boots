using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peeralize.Service.Lex.Expressions;
using Peeralize.Service.Lex.Parsing.Tokenizers;
using Peeralize.Service.Lex.Parsing.Tokens;
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

        [Theory]
        [InlineData(new object[] { "f(g(a.one, c),b)" })]
        public void TokenizeFunction4(string fc)
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
            Assert.IsType<FunctionExpression>(param1.Value);
            Assert.IsType<VariableExpression>(param2.Value);
            Assert.Equal(((FunctionExpression)param1.Value).Name, "g");
            Assert.True(((FunctionExpression) param1.Value).Parameters.Count == 2);
            VariableExpression fnp1Param1= (VariableExpression)((FunctionExpression)param1.Value).Parameters.First().Value;
            VariableExpression fnp1Param2 = (VariableExpression)((FunctionExpression)param1.Value).Parameters.Skip(1).First().Value;
            Assert.Equal("a.one", fnp1Param1.Name);
            Assert.Equal("c", fnp1Param2.Name);
            Assert.Equal("b", ((VariableExpression)param2.Value).Name);
        }

        [Theory]
        [InlineData(new object[] { "max(c, 1 + a.b) / b + d" })]
        public void TokenizeBinaryOp1(string strExpression)
        {
            var tokenizer = new PrecedenceTokenizer();
            var tokens = tokenizer.Tokenize(strExpression).ToList();
            var parser = new Peeralize.Service.Lex.Parsing.Parser();
            parser.Load(tokens);
            var expressions = parser.ReadExpressions().ToList();
            Assert.True(expressions.Count == 1);
            BinaryExpression firstExp = expressions.First() as BinaryExpression;
            Assert.True(firstExp.Token.TokenType == TokenType.Add);
            Assert.IsType<BinaryExpression>(firstExp.Left);
            Assert.IsType<VariableExpression>(firstExp.Right);
            BinaryExpression divisionLeft = firstExp.Left as BinaryExpression;
            Assert.IsType<VariableExpression>(divisionLeft.Right);
            var fn1 = ((FunctionExpression)divisionLeft.Left);
            Assert.Equal("max", fn1.Name);
            Assert.Equal("max(c, 1 + a.b)", fn1.ToString());
            Assert.Equal("b", ((VariableExpression)divisionLeft.Right).Name); 
        }
    }
}
