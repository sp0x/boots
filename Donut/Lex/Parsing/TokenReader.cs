using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Lex.Data;
using Donut.Parsing.Tokens;

namespace Donut.Lex.Parsing
{
    /// <summary>   A token sequence reader helper. </summary>
    ///
    /// <remarks>   Vasko, 25-Dec-17. </remarks>

    public class TokenReader
    {
        public TokenMarker Marker { get; private set; }
        private Stack<DslToken> _tokenSequence;
        private DslToken _lookaheadFirst;
        private DslToken _lookaheadSecond;

        public DslToken Current => _lookaheadFirst;
        public DslToken NextToken => _lookaheadSecond; 
        public bool IsComplete
        {
            get
            {
                return _tokenSequence.Count == 0 && _lookaheadFirst.TokenType == TokenType.EOF;
            }
        }

        public TokenReader(IEnumerable<DslToken> tokens)
        {
            Marker = new TokenMarker();
            Load(tokens);
        }

        private void Load(IEnumerable<DslToken> tokens)
        {
            LoadSequenceStack(tokens);
            PrepareLookaheads();
        }

        private void LoadSequenceStack(IEnumerable<DslToken> tokens)
        {
            if (tokens == null) return;
            _tokenSequence = new Stack<DslToken>();
            //int count = tokens.Count;
            foreach (var token in tokens.Reverse())
            {
                _tokenSequence.Push(token);
            }
        }

        /// <summary>   Gets a copy of this token reader the branch. </summary>
        ///
        /// <remarks>   Vasko, 25-Dec-17. </remarks>
        ///
        /// <returns>   A TokenReader. </returns>

        public TokenReader Branch()
        {
            var treader = new TokenReader(_tokenSequence);
            treader.Marker = Marker.Clone();
            return treader;
        }


        private void PrepareLookaheads()
        {
            if (_tokenSequence.Count == 0)
            {
                _lookaheadFirst = new DslToken(TokenType.EOF, string.Empty, 0);
                return;
            } 
            _lookaheadFirst = _tokenSequence.Pop();
            if (_tokenSequence.Count > 0)
            {
                _lookaheadSecond = _tokenSequence.Pop();
            }
            else
            {
                _lookaheadSecond = new DslToken(TokenType.EOF, string.Empty, _lookaheadFirst.Line);
            }
            
        }

        public DslObject GetObject()
        {
            var token = Current;
            switch (token.TokenType)
            {
                case TokenType.Collection:
                    return DslObject.Collection;
                case TokenType.Type:
                    return DslObject.Type;
                case TokenType.Feature:
                    return DslObject.Feature;
                default:
                    throw new DslParserException("" + token.Value);
            }
        }

        public DslOperator GetOperator()
        {
            var token = Current;
            switch (token.TokenType)
            {
                case TokenType.Equals: return DslOperator.Equals;
                case TokenType.NotEquals: return DslOperator.NotEquals;
                case TokenType.In: return DslOperator.In;
                case TokenType.NotIn: return DslOperator.NotIn;
                case TokenType.Add: return DslOperator.Add;
                case TokenType.Subtract: return DslOperator.Subtract;
                case TokenType.Multiply: return DslOperator.Multiply;
                case TokenType.Divide: return DslOperator.Divide;
                default:
                    throw new DslParserException("Expected =, !=, LIKE, NOT LIKE, IN, NOT IN, /, +, -, * but found: " + token.Value);
            }
        }

        /// <summary>
        /// Seeks untill a predicate matches.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public TokenMarker SeekTo(Predicate<TokenMarker> filter)
        {
            if (Marker.Token!=null && filter(Marker))
            {
                return Marker.Clone();
            }
            TokenMarker matchingMarker = null; 
            foreach (var item in this.AsEnumerable())
            {
                if (filter(item))
                {
                    return item;
                }
            }
            return matchingMarker;
        }

        private IEnumerable<TokenMarker> AsEnumerable()
        {  
            var currentMarker = this.Marker.Clone();
            var prefix = new List<DslToken>() { _lookaheadFirst, _lookaheadSecond };
            var tokenList = new List<DslToken>(prefix.Concat(_tokenSequence).ToArray());
            tokenList.Add(new DslToken(TokenType.EOF, string.Empty, _lookaheadFirst.Line)
            {
                Position = 0
            });
            for (var i=0; i < tokenList.Count(); i++)
            {
                var nextTokens = tokenList.Skip(i + 1);
                var element = tokenList[i];
                currentMarker.SetToken(element);
                currentMarker.SetNextTokens(nextTokens);
                if (element.TokenType == TokenType.OpenParenthesis)
                {
                    currentMarker.Depth++;
                }
                else if (element.TokenType == TokenType.CloseParenthesis)
                {
                    currentMarker.Depth--;
                }
                yield return currentMarker;
            }
        }

        public DslToken ReadToken(TokenType tokenType)
        {
            if (_lookaheadFirst.TokenType != tokenType)
                throw new DslParserException(string.Format("Expected {0} but found: {1}", tokenType.ToString().ToUpper(), _lookaheadFirst.Value));

            return _lookaheadFirst;
        }

        /// <summary>
        /// Discards and returns the current token.
        /// </summary>
        /// <returns></returns>
        public DslToken DiscardToken()
        {
            var token = _lookaheadFirst.Clone();
            _lookaheadFirst = _lookaheadSecond.Clone();

            if (_tokenSequence.Any())
                _lookaheadSecond = _tokenSequence.Pop();
            else
                _lookaheadSecond = new DslToken(TokenType.EOF, string.Empty, _lookaheadFirst.Line)
                {
                    Position = 0
                };
            Marker.SetToken(_lookaheadFirst);
            if (token.TokenType == TokenType.OpenParenthesis)
            {
                Marker.Depth++;
            }
            else if (token.TokenType == TokenType.CloseParenthesis)
            {
                Marker.Depth--;
            }
            return token;
        }

        public DslToken DiscardToken(TokenType tokenType)
        {
            if (_lookaheadFirst.TokenType != tokenType)
                throw new DslParserException(string.Format("Expected {0} but found: {1}",
                    tokenType.ToString().ToUpper(), _lookaheadFirst.Value));

            return DiscardToken();
        }
         
    }
}