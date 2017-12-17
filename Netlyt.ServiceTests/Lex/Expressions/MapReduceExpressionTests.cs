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

            var tokenizer = new PrecedenceTokenizer();
            var parser = new TokenParser(tokenizer.Tokenize(txt));
            var mapReduce = parser.ReadMapReduce();
            var values = mapReduce.ValueMembers.ConcatExpressions();
            var keys = mapReduce.Keys.ConcatExpressions();
            Assert.Equal(expectedKeys, keys);
            Assert.Equal(expectedValues, values);
            var codeGen = new CodeGenerator();
            var emittedBlob = codeGen.GenerateFromExpression(mapReduce);
            emittedBlob = emittedBlob;
            //Generate the code for a map reduce with mongo

        }

    }
}
