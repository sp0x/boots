using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Lex.Expressions;
using Donut.Lex.Parsing;
using Donut.Parsing.Tokenizers;
using Donut.Parsing.Tokens;
using Xunit;

namespace Netlyt.ServiceTests.Lex.Parsing.Tokenizers
{
    public class TokenParserTests
    {

        [Theory]
        [InlineData(new object[]{ "f(a)" })]
        public void TokenizeFunction1(string fc)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var tokens = tokenizer.Tokenize(fc).ToList();
            var parser = new DonutSyntaxReader();
            parser.Load(tokens);
            var x = parser.ReadFunctionCall();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count > 0);
            var parameterExpression = x.Parameters.First();
            Assert.IsType<VariableExpression>(parameterExpression.Value);
            Assert.Equal("a", ((VariableExpression) parameterExpression.Value).Name);
        }

        [Theory]
        [InlineData(new object[]
        {
            "() => 1+1","() => 1 + 1"
        })]
        public void TokenizeLambda(string fc, string expectedLambda)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var tokens = tokenizer.Tokenize(fc).ToList();
            var parser = new DonutSyntaxReader();
            parser.Load(tokens);
            var parsedLambda = parser.ReadExpression();
            Assert.Equal(expectedLambda, parsedLambda.ToString());
        }

        [Theory]
        [InlineData(new object[]{"(a) => a+1", "(a) => a + 1"})]
        [InlineData(new object[]{"(a, b) => a+1", "(a, b) => a + 1"})]
        [InlineData(new object[]{"(a, b, c, d) => max(a+1, b, c, d)", "(a, b, c, d) => max(a + 1, b, c, d)"})]
        [InlineData(new object[]{ "fna((a, b, c, d) => max(a+1, b, c, d))", "fna((a, b, c, d) => max(a + 1, b, c, d))"})] 
        public void TokenizeLambda2(string fc, string expectedLambda)
        { 
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var tokens = tokenizer.Tokenize(fc).ToList();
            var parser = new DonutSyntaxReader();
            parser.Load(tokens);
            var parsedLambda = parser.ReadExpression();
            Assert.Equal(expectedLambda, parsedLambda.ToString());
        }

        [Theory]
        [InlineData(new object[]{"a[0]", "a[0]"})]
        [InlineData(new object[]{"a[0,1]", "a[0, 1]"})]
        [InlineData(new object[]{"a[0,max(1, 2)]", "a[0, max(1, 2)]"})]
        [InlineData(new object[]{"a[0, c[1]]", "a[0, c[1]]"})]
        [InlineData(new object[]{ "events[0].ondate", "events[0].ondate" })]
        public void TestArrayAccess(string code, string expOutcome)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var tokens = tokenizer.Tokenize(code).ToList();
            var parser = new DonutSyntaxReader();
            parser.Load(tokens);
            var exp = parser.ReadExpressions().FirstOrDefault();
            Assert.NotNull(exp);
            Assert.Equal(expOutcome, exp.ToString());
        }

        [Theory]
        //[InlineData(new object[]{ "{ a = 3 }", "{\na = 3\n}"})]
        [InlineData(new object[]{ "{ a = 3;" +
                                  "abc1 = a[0, c[1]] }", "{\na = 3\nabc1 = a[0, c[1]]\n}"})]
        public void TestBlock(string code, string expOutcome)
        {
            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var tokens = tokenizer.Tokenize(code).ToList();
            var parser = new DonutSyntaxReader();
            parser.Load(tokens);
            var exp = parser.ReadExpressions().FirstOrDefault();
            Assert.NotNull(exp);
            var enumerable = expOutcome;
            var replace = exp.ToString();
            Assert.Equal(enumerable, replace);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        [Theory]
        [InlineData(new object[]{ "a + b + c"})]
        public void ParseMultipleBinaryOps(string content)
        {  
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(content));
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
            var expression = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(content)).ReadOrderBy();
            Assert.Equal(expected, expression.ByClause.ConcatExpressions());
        }

        [Theory]
        [InlineData(new object[] { "(a)", "a" })]
        [InlineData(new object[] { "(a + b)", "a + b" })]
        [InlineData(new object[] { "(max(a + b))", "max(a + b)" })]
        public void ReadParenthesisContentTest(string content, string expected)
        {
            var expressions = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(content)).ReadParenthesisContent(); 
            var expString = String.Join(", ", expressions);
            Assert.Equal(expected, expString);
        }



        [Theory]
        [InlineData(new object[] { "f(a,b)" })]
        [InlineData(new object[] { "f(a,b,c)" })]
        public void TokenizeFunction2(string fc)
        {
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(fc));   
            var x = parser.ReadFunctionCall();
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
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(fc));
            var x = parser.ReadFunctionCall();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count >= 2);
            var param1 = x.Parameters.First();
            var param2 = x.Parameters.Skip(1).First();
            Assert.IsType<VariableExpression>(param1.Value);
            Assert.IsType<VariableExpression>(param2.Value);
            Assert.Equal("a.one",((VariableExpression)param1.Value).ToString()); 
            Assert.Equal("b", ((VariableExpression)param2.Value).Name); 
        }

        [Theory]
        [InlineData(new object[] { "f(g(a.one, c),b)" })]
        public void TokenizeFunction4(string fc)
        {
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(fc));
            var x = parser.ReadFunctionCall();
            Assert.Equal("f", x.Name);
            Assert.True(x.Parameters.Count >= 2);
            var param1 = x.Parameters.First();
            var param2 = x.Parameters.Skip(1).First();
            Assert.IsType<CallExpression>(param1.Value);
            Assert.IsType<VariableExpression>(param2.Value);
            Assert.Equal("g", ((CallExpression)param1.Value).Name);
            Assert.True(((CallExpression) param1.Value).Parameters.Count == 2);
            VariableExpression fnp1Param1= (VariableExpression)((CallExpression)param1.Value).Parameters.First().Value;
            VariableExpression fnp1Param2 = (VariableExpression)((CallExpression)param1.Value).Parameters.Skip(1).First().Value;
            Assert.Equal("a.one", fnp1Param1.ToString());
            Assert.Equal("c", fnp1Param2.Name);
            Assert.Equal("b", ((VariableExpression)param2.Value).Name);
        }

        [Theory]
        [InlineData(new object[] { "max(c, 1 + a.b) / b + d" })]
        public void TokenizeBinaryOp1(string strExpression)
        {
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(strExpression));
            var expressions = parser.ReadExpressions().ToList();
            Assert.True(expressions.Count == 1);
            BinaryExpression firstExp = expressions.First() as BinaryExpression;
            Assert.True(firstExp.Token.TokenType == TokenType.Add);
            Assert.IsType<BinaryExpression>(firstExp.Left);
            Assert.IsType<VariableExpression>(firstExp.Right);
            BinaryExpression divisionLeft = firstExp.Left as BinaryExpression;
            Assert.IsType<VariableExpression>(divisionLeft.Right);
            var fn1 = ((CallExpression)divisionLeft.Left);
            Assert.Equal("max", fn1.Name);
            Assert.Equal("max(c, 1 + a.b)", fn1.ToString());
            Assert.Equal("b", ((VariableExpression)divisionLeft.Right).Name); 
        }

        [Theory]
        [InlineData(new object[] { "a.b", "a.b" })]
        [InlineData(new object[] { "a.b.c", "a.b.c" })]
        public void TokenizeVariableWithMember(string content, string expected)
        {
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(content));
            var expression = parser.ReadVariable();
            Assert.Equal(expected, expression.ToString());
        }

        [Theory]
        [InlineData(new object[] { "a.b", "b" })]
        [InlineData(new object[] { "a.b.c", "b" })]
        public void TokenizeFirstMemberAccess(string content, string expected)
        {
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(content));
            var expression = parser.ReadVariable();
            MemberExpression member = expression.Member;
            Assert.Equal(expected, member.Parent.ToString());
        }

        [Theory]
        [InlineData(new object[] { "a.b", "b" })]
        [InlineData(new object[] { "a.b.c", "c" })]
        [InlineData(new object[] { "a.b.c.d", "d" })]
        public void TokenizeLastMemberAccess(string content, string expected)
        {
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(content));
            var expression = parser.ReadVariable();
            Assert.NotNull(expression.Member);
            var root = expression.Member;
            var currentElement = root;
            while(currentElement.ChildMember != null)
            {
                currentElement = (MemberExpression)currentElement.ChildMember;
            }
            Assert.Equal(expected, ((MemberExpression)currentElement).Parent.ToString());
        }

        [Theory]
        [InlineData(new object[] { "a = time(now()) / 1000", "time(now()) / 1000" })]
        [InlineData(new object[] { "a = time(now()) / 1000 - 10", "time(now()) / 1000 - 10" })]
        [InlineData(new object[] { "a = time(now()) / 1000, 10", "time(now()) / 1000" })]
        public void TokenizeAssignment(string content, string expectedValue)
        {
            var parser = new DonutSyntaxReader(new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(content));
            var expression = parser.ReadAssign();
            Assert.Equal(expectedValue, expression.Value.ToString()); 
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
            var tokens = new PrecedenceTokenizer(new DonutTokenDefinitions()).Tokenize(txt).ToList();
            Assert.True(tokens.Any(), "No tokens found");
            Assert.True(tokens.Count == count, $"Incorrect token count, they must be {count}");
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
            reduce day = time_s(input.ondate) / (60*60*24), 
                   uuid = input.uuid
            reduce_map  ondate = input.ondate,
                        value = input.value,
                        type = input.type
                    ",
            "day = time_s(input.ondate) / 60 * 60 * 24, uuid = input.uuid",
            "ondate = input.ondate, value = input.value, type = input.type"
        })]
        public void ParseMapReduce(
            string txt,
            string expectedKeys,
            string expectedValues)
        {

            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(txt));
            var mapReduce = parser.ReadMapReduce();
            var values = mapReduce.ValueMembers.ConcatExpressions();
            var keys = mapReduce.Keys.ConcatExpressions();
            Assert.Equal(expectedKeys, keys);
            Assert.Equal(expectedValues, values);
        }

        /// <summary>
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="expectedFeatureTypeName"></param>
        /// <param name="expectedSource"></param>
        /// <param name="expectedPreSort"></param>
        /// <param name="expectedFeatureKVP"></param>
        [Theory]
        [InlineData(new object[] { @"
            define User
            from events
            order by uuid, time
            set User.id=unique(events.uuid)
            set User.visit_count=count(events.value)",
            "User", "uuid, time",
            new string[] {
                "User.id", "unique(events.uuid)",
                "User.visit_count", "count(events.value)" }
        })]
        public void ParseFeatureDefinition(
            string txt, 
            string expectedFeatureTypeName, 
            string expectedPreSort,
            string[] expectedFeatureKVP)
        {

            var tokenizer = new PrecedenceTokenizer(new DonutTokenDefinitions());
            var parser = new DonutSyntaxReader(tokenizer.Tokenize(txt));
            var model = parser.ParseDonutScript();
            Assert.NotNull(model);
            Assert.NotNull(model.Features);
            Assert.NotNull(model.Type);
            Assert.Equal(expectedFeatureTypeName, model.Type.Name);
            var sortClauses = model.StartingOrderBy.ByClause.ConcatExpressions(); 
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
