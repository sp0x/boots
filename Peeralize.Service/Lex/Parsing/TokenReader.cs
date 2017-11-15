using System;
using System.Collections.Generic;
using System.Linq;
using Peeralize.Service.Lex.Data;
using Peeralize.Service.Lex.Parsing.Tokens;

namespace Peeralize.Service.Lex.Parsing
{
    public class TokenReader
    {
        public TokenCursor Cursor { get; private set; }
        private Stack<DslToken> _tokenSequence;
        private DslToken _lookaheadFirst;
        private DslToken _lookaheadSecond;

        public DslToken Current => _lookaheadFirst;
        public DslToken NextToken => _lookaheadSecond; 
        public bool IsComplete
        {
            get
            {
                return _tokenSequence.Count == 0 && _lookaheadSecond.TokenType == TokenType.EOF;
            }
        }

        public TokenReader(List<DslToken> tokens)
        {
            Cursor = new TokenCursor();
            Load(tokens);
        }

        private void Load(List<DslToken> tokens)
        {
            LoadSequenceStack(tokens);
            PrepareLookaheads();
        }

        private void LoadSequenceStack(List<DslToken> tokens)
        {
            _tokenSequence = new Stack<DslToken>();
            int count = tokens.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                _tokenSequence.Push(tokens[i]);
            }
        } 
        private void PrepareLookaheads()
        {
            _lookaheadFirst = _tokenSequence.Pop();
            _lookaheadSecond = _tokenSequence.Pop();
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
                case TokenType.Equals:
                    return DslOperator.Equals;
                case TokenType.NotEquals:
                    return DslOperator.NotEquals;
                case TokenType.In:
                    return DslOperator.In;
                case TokenType.NotIn:
                    return DslOperator.NotIn;
                default:
                    throw new DslParserException("Expected =, !=, LIKE, NOT LIKE, IN, NOT IN but found: " + token.Value);
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
                _lookaheadSecond = new DslToken(TokenType.EOF, string.Empty, 0);
            Cursor.SetToken(_lookaheadFirst);
            if (token.TokenType == TokenType.OpenParenthesis)
            {
                Cursor.Depth++;
            }
            else if (token.TokenType == TokenType.CloseParenthesis)
            {
                Cursor.Depth--;
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