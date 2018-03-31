using System.Collections.Generic;
using System.Text.RegularExpressions;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing.Tokenizers
{
    public class TokenDefinition
    {
        private Regex _regex;
        private readonly TokenType _returnsToken;
        private readonly int _precedence;
        public TokenType Type => _returnsToken;

        public TokenDefinition(TokenType returnsToken, string regexPattern, int precedence)
        {
            _regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _returnsToken = returnsToken;
            _precedence = precedence;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public IEnumerable<TokenMatch> FindMatches(string inputString)
        {
            if(string.IsNullOrEmpty(inputString)) yield break;
            var matches = _regex.Matches(inputString);
            for (int i = 0; i < matches.Count; i++)
            {
                yield return new TokenMatch()
                {
                    StartIndex = matches[i].Index,
                    EndIndex = matches[i].Index + matches[i].Length,
                    TokenType = _returnsToken,
                    Value = matches[i].Value,
                    Precedence = _precedence
                };
            }
        }
    }
}