using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing.Tokenizers
{
    /// <summary>
    /// Tokenizes text based on precedence
    /// </summary>
    public class PrecedenceTokenizer : ITokenizer
    {
        private List<TokenDefinition> _tokenDefinitions;

        public PrecedenceTokenizer(TokenDefinitionCollection toks)
        {
            _tokenDefinitions = new List<TokenDefinition>();
            _tokenDefinitions.AddRange(toks.GetAll());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public IEnumerable<DslToken> Tokenize(string text)
        {
            var reader = new StringReader(text);
            return Tokenize(reader);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strReader"></param>
        /// <returns></returns>
        public IEnumerable<DslToken> Tokenize(StringReader strReader)
        {
            var tokenMatches = FindTokenMatches(strReader);

            var groupedByIndex = tokenMatches.GroupBy(x => new TokenPosition(x.Line, (uint)x.StartIndex), 
                new TokenPositionComparer())
                .OrderBy(x => x.Key.Line)
                .ThenBy(x=>x.Key.Position)
                .ToList();

            TokenMatch lastMatch = null;
            for (int i = 0; i < groupedByIndex.Count; i++)
            {
                var orderedEnumerable = groupedByIndex[i].OrderBy(x => x.Precedence);
                var bestMatch = orderedEnumerable.First();
                if (lastMatch != null && bestMatch.StartIndex < lastMatch.EndIndex 
                    && bestMatch.Line == lastMatch.Line)
                {
                    continue;
                }
                var token = new DslToken(bestMatch.TokenType, bestMatch.Value, bestMatch.Line)
                {
                    Position = (uint)bestMatch.StartIndex
                };
                yield return token;

                lastMatch = bestMatch;
            } 
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lqlText"></param>
        /// <returns></returns>
        private IEnumerable<TokenMatch> FindTokenMatches(StringReader lqlText)
        {
            //var tokenMatches = new List<TokenMatch>();
            string line;
            uint iLine = 1;
            int foundTokens = 0; 
            while (null != (line = lqlText.ReadLine()))
            {
                foreach (var tokenDefinition in _tokenDefinitions)
                {
                    var tokenMatches = tokenDefinition.FindMatches(line).ToList();
                    foreach (var match in tokenMatches)
                    {
                        match.Line = iLine;
                        yield return match;
                        foundTokens++;
                    }
                    //tokenMatches.AddRange(collection);
                }
                iLine++;
            }
            if (foundTokens == 0)
            {
                foreach (var tokenDefinition in _tokenDefinitions)
                {
                    var tokenMatches = tokenDefinition.FindMatches(line).ToList();
                    foreach (var match in tokenMatches)
                    {
                        match.Line = iLine;
                        yield return match;
                        foundTokens++;
                    }
                    //tokenMatches.AddRange(collection);
                }
            }
            //return tokenMatches;
        }


    }
}
