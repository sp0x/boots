using System.Collections.Generic;
using System.IO;
using System.Linq;
using Peeralize.Service.Lex.Data;
using Peeralize.Service.Lex.Parsing.Tokens;

namespace Peeralize.Service.Lex.Parsing.Tokenizers
{
    public class PrecedenceTokenizer : ITokenizer
    {
        private List<TokenDefinition> _tokenDefinitions;

        public PrecedenceTokenizer()
        {
            _tokenDefinitions = new List<TokenDefinition>();

            _tokenDefinitions.Add(new TokenDefinition(TokenType.And, "(^|\\W)and(?=[\\s\\t])", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Between, "between", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.CloseParenthesis, "\\)", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Comma, ",", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.MemberAccess, "\\.", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Assign, "=", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Equals, "==", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NotEquals, "!=", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Define, "define", 1)); //(?<=define\\s)([\\w\\d_]+)
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NotIn, "not\\sin", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.In, "(^|\\W)in(?=[\\s\\t])", 1));
            //_tokenDefinitions.Add(new TokenDefinition(TokenType.Like, "like", 1));
            //_tokenDefinitions.Add(new TokenDefinition(TokenType.Limit, "limit", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.From, "(^|\\W)from(?=[\\s\\t])", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.OrderBy, "(^|\\W)order\\sby", 1));
            //_tokenDefinitions.Add(new TokenDefinition(TokenType.Message, "msg|message", 1));
            //_tokenDefinitions.Add(new TokenDefinition(TokenType.NotLike, "not like", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.OpenParenthesis, "\\(", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Or, "or", 1)); 
            _tokenDefinitions.Add(new TokenDefinition(TokenType.DateTimeValue, "\\d\\d\\d\\d-\\d\\d-\\d\\d \\d\\d:\\d\\d:\\d\\d", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.StringValue, "'([^']*)'", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NumberValue, "\\d+", 2));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Set, "(^|\\s)(set)(?=[\\s\t])", 2));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Symbol, "[\\w\\d_]+", 3));
        }

        public IEnumerable<DslToken> Tokenize(string text)
        {
            var reader = new StringReader(text);
            return Tokenize(reader);
        }
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

                yield return new DslToken(bestMatch.TokenType, bestMatch.Value, bestMatch.Line);

                lastMatch = bestMatch;
            } 
        }

        private IEnumerable<TokenMatch> FindTokenMatches(StringReader lqlText)
        {
            //var tokenMatches = new List<TokenMatch>();
            string line;
            uint iLine = 1;
            while (null != (line = lqlText.ReadLine()))
            {
                foreach (var tokenDefinition in _tokenDefinitions)
                {
                    var tokenMatches = tokenDefinition.FindMatches(line).ToList();
                    foreach (var match in tokenMatches)
                    {
                        match.Line = iLine;
                        yield return match;
                    }
                    //tokenMatches.AddRange(collection);
                }
                iLine++;
            } 
            //return tokenMatches;
        }


    }
}
