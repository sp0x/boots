using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generation;
using Netlyt.Service.Lex.Parsing;
using Netlyt.Service.Lex.Parsing.Tokenizers;
using Xunit;

namespace Netlyt.ServiceTests.Lex.Expressions
{
    public class MapReduceExpressionTests
    {
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
            var tokenizer = new PrecedenceTokenizer();
            var parser = new TokenParser(tokenizer.Tokenize(txt));
            var mapReduce = parser.ReadMapReduce();
            var values = mapReduce.ValueMembers.ConcatExpressions();
            var keys = mapReduce.Keys.ConcatExpressions();
            Assert.Equal(expectedKeys, keys);
            Assert.Equal(expectedValues, values);
            var codeGen = new CodeGenerator();
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
                events = selectMany(values, x => x.events),
                uuid = key.uuid,
                day = key.day,
                noticed_date = if any(events) events[0].ondate else null",
            ""
        })]
        public void ParseMapReduceAggregate(string code, string expectedAggregate)
        {
            var tokenizer = new PrecedenceTokenizer();
            var parser = new TokenParser(tokenizer.Tokenize(code));
            var mapReduce = parser.ReadMapReduce();
            var values = mapReduce.ValueMembers.ConcatExpressions();
            var keys = mapReduce.Keys.ConcatExpressions();
            var codeGen = new CodeGenerator();
            var emittedBlob = codeGen.GenerateFromExpression(mapReduce);
            Assert.True(emittedBlob.Length > 100);
        }

    }
}
