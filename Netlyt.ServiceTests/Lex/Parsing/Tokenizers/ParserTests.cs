using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Parsing;
using Netlyt.Service.Lex.Parsing.Tokenizers;
using Netlyt.Service.Lex.Parsing.Tokens;
using Xunit;

namespace Netlyt.ServiceTests.Lex.Parsing
{
    public class TokenParserTests
    {

        [Theory]
        [InlineData(new object[]{ "f(a)" })]
        public void TokenizeFunction1(string fc)
        {
            var tokenizer = new PrecedenceTokenizer();
            var tokens = tokenizer.Tokenize(fc).ToList();
            var parser = new Netlyt.Service.Lex.Parsing.TokenParser();
            parser.Load(tokens);
            var x = parser.ReadFunction();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count > 0);
            var parameterExpression = x.Parameters.First();
            Assert.IsType<VariableExpression>(parameterExpression.Value);
            Assert.Equal(((VariableExpression) parameterExpression.Value).Name, "a");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        [Theory]
        [InlineData(new object[]{ "a + b + c"})]
        public void ParseMultipleBinaryOps(string content)
        {  
            var parser = new TokenParser(new PrecedenceTokenizer().Tokenize(content));
            var exps = parser.ReadExpressions().ToList();
            Assert.True(exps.Count == 1);
            Assert.IsType<BinaryExpression>(exps.FirstOrDefault());
            BinaryExpression firstExp = exps.FirstOrDefault() as BinaryExpression;
            Assert.NotNull(firstExp);
            Assert.Equal("a + b", firstExp.Left.ToString());
            Assert.Equal("c", firstExp.Right.ToString()); 
        }

        [Theory]
        [InlineData(new object[]{"order by a + b ", "a + b"})]
        [InlineData(new object[]{ "order by (a + b)", "a + b"})]
        [InlineData(new object[] { "order by max(a + b) + fn1(c, d, min(a,b))", "max(a + b) + fn1(c, d, min(a, b))" })]
        [InlineData(new object[] { "order by a, b", "a, b" })]
        public void ReadOrderByTest(string content, string expected)
        { 
            var expression = new TokenParser(new PrecedenceTokenizer().Tokenize(content)).ReadOrderBy();
            Assert.Equal(expected, expression.ByClause.ConcatTokens());
        }

        [Theory]
        [InlineData(new object[] { "(a)", "a" })]
        [InlineData(new object[] { "(a + b)", "a + b" })]
        [InlineData(new object[] { "(max(a + b))", "max(a + b)" })]
        public void ReadParenthesisContentTest(string content, string expected)
        {
            var expressions = new TokenParser(new PrecedenceTokenizer().Tokenize(content)).ReadParenthesisContent(); 
            var expString = String.Join(", ", expressions);
            Assert.Equal(expected, expString);
        }



        [Theory]
        [InlineData(new object[] { "f(a,b)" })]
        [InlineData(new object[] { "f(a,b,c)" })]
        public void TokenizeFunction2(string fc)
        {
            var parser = new TokenParser(new PrecedenceTokenizer().Tokenize(fc));   
            var x = parser.ReadFunction();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count >= 2);
            var param1 = x.Parameters.First();
            var param2 = x.Parameters.Skip(1).First();
            Assert.IsType<VariableExpression>(param1.Value);
            Assert.IsType<VariableExpression>(param2.Value);
            Assert.Equal("a", ((VariableExpression) param1.Value).Name);
            Assert.Equal("b",((VariableExpression) param2.Value).Name);
        }

        [Theory]
        [InlineData(new object[] { "f(a.one,b)" })] 
        public void TokenizeFunction3(string fc)
        {
            var parser = new TokenParser(new PrecedenceTokenizer().Tokenize(fc));
            var x = parser.ReadFunction();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count >= 2);
            var param1 = x.Parameters.First();
            var param2 = x.Parameters.Skip(1).First();
            Assert.IsType<VariableExpression>(param1.Value);
            Assert.IsType<VariableExpression>(param2.Value);
            Assert.Equal(((VariableExpression)param1.Value).ToString(), "a.one"); 
            Assert.Equal(((VariableExpression)param2.Value).Name, "b"); 
        }

        [Theory]
        [InlineData(new object[] { "f(g(a.one, c),b)" })]
        public void TokenizeFunction4(string fc)
        {
            var parser = new TokenParser(new PrecedenceTokenizer().Tokenize(fc));
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
            Assert.Equal("a.one", fnp1Param1.ToString());
            Assert.Equal("c", fnp1Param2.Name);
            Assert.Equal("b", ((VariableExpression)param2.Value).Name);
        }

        [Theory]
        [InlineData(new object[] { "max(c, 1 + a.b) / b + d" })]
        public void TokenizeBinaryOp1(string strExpression)
        {
            var parser = new TokenParser(new PrecedenceTokenizer().Tokenize(strExpression));
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

        [Theory]
        [InlineData(new object[] { "a.b", "a.b" })]
        [InlineData(new object[] { "a.b.c", "a.b.c" })]
        public void TokenizeVariableWithMember(string content, string expected)
        {
            var parser = new TokenParser(new PrecedenceTokenizer().Tokenize(content));
            var expression = parser.ReadVariable();
            Assert.Equal(expected, expression.ToString());
        }

        [Theory]
        [InlineData(new object[] { "a.b", "b" })]
        [InlineData(new object[] { "a.b.c", "b" })]
        public void TokenizeFirstMemberAccess(string content, string expected)
        {
            var parser = new TokenParser(new PrecedenceTokenizer().Tokenize(content));
            var expression = parser.ReadVariable();
            Assert.Equal(expected, expression.Member.Name);
        }

        [Theory]
        [InlineData(new object[] { "a.b", "b" })]
        [InlineData(new object[] { "a.b.c", "c" })]
        [InlineData(new object[] { "a.b.c.d", "d" })]
        public void TokenizeLastMemberAccess(string content, string expected)
        {
            var parser = new TokenParser(new PrecedenceTokenizer().Tokenize(content));
            var expression = parser.ReadVariable();
            Assert.NotNull(expression.Member);
            var root = expression.Member;
            var currentElement = root;
            while(currentElement.ChildMember != null)
            {
                currentElement = currentElement.ChildMember;
            }
            Assert.Equal(expected, currentElement.Name);
        }

        /// <summary>
        /// Test basic parsing
        /// </summary>
        /// <param name="inputDirectory"></param>
        [Theory]
        [InlineData(@"
            define User
            from events
            order by(uuid,time)
            User.id=unique(events.uuid)
            User.visit_count=count(events.value)")]
        public void TokenizeSimpleFeatureDefinition(string txt)
        { 
            var count = 30;
            var tokens = new PrecedenceTokenizer().Tokenize(txt).ToList();
            Assert.True(tokens.Any(), "No tokens found");
            Assert.True(tokens.Count == count, $"Incorrect token count, they must be {count}");
        }

        /// <summary>
        /// </summary>
        /// <param name="inputDirectory"></param>
        [Theory]
        [InlineData(new object[] { @"
            define User
            from events
            order by uuid, time
            set User.id=unique(events.uuid)
            set User.visit_count=count(events.value)",
            "User", "events", "uuid, time",
            new string[] {
                "User.id", "unique(events.uuid)",
                "User.visit_count", "count(events.value)" }
        })]
        public void ParseFeatureDefinition(
            string txt, 
            string expectedFeatureTypeName,
            string expectedSource,
            string expectedPreSort,
            string[] expectedFeatureKVP)
        {
            var tokenizer = new PrecedenceTokenizer();
            var parser = new TokenParser(tokenizer.Tokenize(txt));
            var model = parser.ParseModel();
            Assert.NotNull(model);
            Assert.NotNull(model.Features);
            Assert.NotNull(model.Type);
            Assert.Equal(expectedFeatureTypeName, model.Type.Name);
            var sortClauses = model.StartingOrderBy.ByClause.ConcatTokens(); 
            Assert.Equal(expectedPreSort, sortClauses);
            for (var i = 0; i < expectedFeatureKVP.Length; i += 2)
            {
                var expName = expectedFeatureKVP[i];
                var expValue = expectedFeatureKVP[i + 1];
                Assert.Contains(model.Features, x =>
                {
                    var nameMatch = x.Member.ToString() == expName;
                    var valMatch = x.Value.ToString() == expValue;
                    return nameMatch && valMatch;
                });
            } 
        }
    }
}
