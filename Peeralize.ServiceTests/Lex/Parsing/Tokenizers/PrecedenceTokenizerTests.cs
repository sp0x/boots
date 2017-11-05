using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peeralize.Service.Lex.Parsing.Tokenizers;
using Peeralize.Service.Lex.Parsing.Tokens;
using Xunit;

namespace Peeralize.ServiceTests.Lex.Parsing.Tokenizers
{
    public class PrecedenceTokenizerTests
    {

        /// <summary>
        /// Test basic parsing
        /// </summary>
        /// <param name="inputDirectory"></param>
        [Theory]
        [InlineData(new object[] { @"
            define User
            from events
            order by(uuid,time)
            User.id=unique(events.uuid)
            User.visit_count=count(events.value)" })]
        public void Tokenize(string txt)
        {
            var tokenizer = new PrecedenceTokenizer(); 
            var tokens = tokenizer.Tokenize(txt).ToList();
            Assert.True(tokens.Any());
            Assert.True(tokens.Count == 16);
        }

        /// <summary>
        /// </summary>
        /// <param name="inputDirectory"></param>
        [Theory]
        [InlineData(new object[] { @"
            define User
            from events
            order by(uuid,time)
            User.id=unique(events.uuid)
            User.visit_count=count(events.value)" })]
        public void GenerateQuery(string txt)
        {
            var tokenizer = new PrecedenceTokenizer();
            var parser = new Peeralize.Service.Lex.Parsing.Parser();
            var items = tokenizer.Tokenize(txt);
            var tokens = items.ToList();
            foreach (var token in tokens)
                Console.WriteLine(string.Format("TokenType: {0}, Value: {1}", token.TokenType, token.Value));
            var x = parser.Parse(tokens);
            x = x;
        }
    }
}
