using System.Collections.Generic;
using System.Linq;
using Peeralize.Service.Lex.Parsing.Tokens;

namespace Peeralize.Service.Lex.Parsing.Tokenizers
{
    public class PrecedenceTokenizer : ITokenizer
    {
        private List<TokenDefinition> _tokenDefinitions;

        public PrecedenceTokenizer()
        {
            _tokenDefinitions = new List<TokenDefinition>();

            _tokenDefinitions.Add(new TokenDefinition(TokenType.And, "and", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Between, "between", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.CloseParenthesis, "\\)", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Comma, ",", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.MemberAccess, "\\.", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Assign, "=", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Equals, "==", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NotEquals, "!=", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Define, "define", 1)); //(?<=define\\s)([\\w\\d_]+)
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NotIn, "not\\sin", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.In, "in", 1));
            //_tokenDefinitions.Add(new TokenDefinition(TokenType.Like, "like", 1));
            //_tokenDefinitions.Add(new TokenDefinition(TokenType.Limit, "limit", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.From, "from", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.OrderBy, "order\\sby", 1));
            //_tokenDefinitions.Add(new TokenDefinition(TokenType.Message, "msg|message", 1));
            //_tokenDefinitions.Add(new TokenDefinition(TokenType.NotLike, "not like", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.OpenParenthesis, "\\(", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Or, "or", 1)); 
            _tokenDefinitions.Add(new TokenDefinition(TokenType.DateTimeValue, "\\d\\d\\d\\d-\\d\\d-\\d\\d \\d\\d:\\d\\d:\\d\\d", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.StringValue, "'([^']*)'", 1));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.NumberValue, "\\d+", 2));
            _tokenDefinitions.Add(new TokenDefinition(TokenType.Symbol, "[\\w\\d_]+", 2));
        }

        public IEnumerable<DslToken> Tokenize(string lqlText)
        {
            var tokenMatches = FindTokenMatches(lqlText);

            var groupedByIndex = tokenMatches.GroupBy(x => x.StartIndex)
                .OrderBy(x => x.Key)
                .ToList();

            TokenMatch lastMatch = null;
            for (int i = 0; i < groupedByIndex.Count; i++)
            {
                var bestMatch = groupedByIndex[i].OrderBy(x => x.Precedence).First();
                if (lastMatch != null && bestMatch.StartIndex < lastMatch.EndIndex)
                    continue;

                yield return new DslToken(bestMatch.TokenType, bestMatch.Value);

                lastMatch = bestMatch;
            } 
        }

        private List<TokenMatch> FindTokenMatches(string lqlText)
        {
            var tokenMatches = new List<TokenMatch>();

            foreach (var tokenDefinition in _tokenDefinitions)
            {
                tokenMatches.AddRange(tokenDefinition.FindMatches(lqlText).ToList());
            }

            return tokenMatches;
        }


    }
}
