using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Peeralize.Service.Lex.Data;
using Peeralize.Service.Lex.Parsing.Tokens;

namespace Peeralize.Service.Lex.Parsing
{
    public class Parser
    {
        private Stack<DslToken> _tokenSequence;
        private DslToken _lookaheadFirst;
        private DslToken _lookaheadSecond;

        private DslFeatureModel _featureModel;
        private MatchCondition _currentMatchCondition;

        private const string ExpectedObjectErrorText = "Expected =, !=, IN or NOT IN but found: ";

        public DslFeatureModel Parse(List<DslToken> tokens)
        {
            LoadSequenceStack(tokens);
            PrepareLookaheads();
            _featureModel = new DslFeatureModel();

            Define();

            //DiscardToken(TokenType.SequenceTerminator);

            return _featureModel;
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

        private DslToken ReadToken(TokenType tokenType)
        {
            if (_lookaheadFirst.TokenType != tokenType)
                throw new DslParserException(string.Format("Expected {0} but found: {1}", tokenType.ToString().ToUpper(), _lookaheadFirst.Value));

            return _lookaheadFirst;
        }

        private void DiscardToken()
        {
            _lookaheadFirst = _lookaheadSecond.Clone();

            if (_tokenSequence.Any())
                _lookaheadSecond = _tokenSequence.Pop();
            else
                _lookaheadSecond = new DslToken(TokenType.SequenceTerminator, string.Empty);
        }

        private void DiscardToken(TokenType tokenType)
        {
            if (_lookaheadFirst.TokenType != tokenType)
                throw new DslParserException(string.Format("Expected {0} but found: {1}",
                    tokenType.ToString().ToUpper(), _lookaheadFirst.Value));

            DiscardToken();
        }

        private void Define()
        {
            DiscardToken(TokenType.Define);
            var newSymbolName = ReadToken(TokenType.Symbol); 
            _featureModel.Type = new FeatureTypeModel()
            {
                Name = newSymbolName.Value
            };
            DiscardToken(TokenType.Symbol);
            ReadFrom();
//            _featureModel.DateRange = new DateRange();
//            _featureModel.DateRange.From = DateTime.ParseExact(ReadToken(TokenType.DateTimeValue).Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private void ReadFrom()
        {
            DiscardToken(TokenType.From);
            var fromCollections = new List<string>();
            do
            {
                var symbol = ReadToken(TokenType.Symbol);
                DiscardToken(TokenType.Symbol);
                fromCollections.Add(symbol.Value);
                if (_lookaheadFirst.TokenType != TokenType.Comma) break;
            } while (true);

            ReadToken(TokenType.Symbol);

        }

        private void ReadType()
        {
            DiscardToken(TokenType.Type); 
        }

        private void MatchCondition()
        {
            CreateNewMatchCondition();

            if (IsObject(_lookaheadFirst))
            {
                if (IsEqualityOperator(_lookaheadSecond))
                {
                    EqualityMatchCondition();
                }
                else if (_lookaheadSecond.TokenType == TokenType.In)
                {
                    InCondition();
                }
                else if (_lookaheadSecond.TokenType == TokenType.NotIn)
                {
                    NotInCondition();
                }
                else
                {
                    throw new DslParserException(ExpectedObjectErrorText + " " + _lookaheadSecond.Value);
                }

                MatchConditionNext();
            }
            else
            {
                throw new DslParserException(ExpectedObjectErrorText + _lookaheadFirst.Value);
            }
        }

        private void EqualityMatchCondition()
        {
            _currentMatchCondition.Object = GetObject(_lookaheadFirst);
            DiscardToken();
            _currentMatchCondition.Operator = GetOperator(_lookaheadFirst);
            DiscardToken();
            _currentMatchCondition.Value = _lookaheadFirst.Value;
            DiscardToken();
        }

        private DslObject GetObject(DslToken token)
        {
            switch (token.TokenType)
            {
                case TokenType.Collection:
                    return DslObject.Collection;
                case TokenType.Type:
                    return DslObject.Type;
                case TokenType.Feature:
                    return DslObject.Feature; 
                default:
                    throw new DslParserException(ExpectedObjectErrorText + token.Value);
            }
        }

        private DslOperator GetOperator(DslToken token)
        {
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

        private void NotInCondition()
        {
            ParseInCondition(DslOperator.NotIn);
        }

        private void InCondition()
        {
            ParseInCondition(DslOperator.In);
        }

        private void ParseInCondition(DslOperator inOperator)
        {
            _currentMatchCondition.Operator = inOperator;
            _currentMatchCondition.Values = new List<string>();
            _currentMatchCondition.Object = GetObject(_lookaheadFirst);
            DiscardToken();

            if (inOperator == DslOperator.In)
                DiscardToken(TokenType.In);
            else if (inOperator == DslOperator.NotIn)
                DiscardToken(TokenType.NotIn);

            DiscardToken(TokenType.OpenParenthesis);
            StringLiteralList();
            DiscardToken(TokenType.CloseParenthesis);
        }

        private void StringLiteralList()
        {
            _currentMatchCondition.Values.Add(ReadToken(TokenType.StringValue).Value);
            DiscardToken(TokenType.StringValue);
            StringLiteralListNext();
        }

        private void StringLiteralListNext()
        {
            if (_lookaheadFirst.TokenType == TokenType.Comma)
            {
                DiscardToken(TokenType.Comma);
                _currentMatchCondition.Values.Add(ReadToken(TokenType.StringValue).Value);
                DiscardToken(TokenType.StringValue);
                StringLiteralListNext();
            }
            else
            {
                // nothing
            }
        }

        private void MatchConditionNext()
        {
            if (_lookaheadFirst.TokenType == TokenType.And)
            {
                AndMatchCondition();
            }
            else if (_lookaheadFirst.TokenType == TokenType.Or)
            {
                OrMatchCondition();
            }
            else if (_lookaheadFirst.TokenType == TokenType.Between)
            {
                DateCondition();
            }
            else
            {
                throw new DslParserException("Expected AND, OR or BETWEEN but found: " + _lookaheadFirst.Value);
            }
        }

        private void AndMatchCondition()
        {
            _currentMatchCondition.LogOpToNextCondition = DslLogicalOperator.And;
            DiscardToken(TokenType.And);
            MatchCondition();
        }

        private void OrMatchCondition()
        {
            _currentMatchCondition.LogOpToNextCondition = DslLogicalOperator.Or;
            DiscardToken(TokenType.Or);
            MatchCondition();
        }

        private void DateCondition()
        {
            DiscardToken(TokenType.Between);

//            _featureModel.DateRange = new DateRange();
//            _featureModel.DateRange.From = DateTime.ParseExact(ReadToken(TokenType.DateTimeValue).Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
//            DiscardToken(TokenType.DateTimeValue);
//            DiscardToken(TokenType.And);
//            _featureModel.DateRange.To = DateTime.ParseExact(ReadToken(TokenType.DateTimeValue).Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
//            DiscardToken(TokenType.DateTimeValue);
//            DateConditionNext();
        }

        private void DateConditionNext()
        {
            if (_lookaheadFirst.TokenType == TokenType.SequenceTerminator)
            {
                // nothing
            }
            else
            {
                throw new DslParserException("Expected LIMIT or the end of the query but found: " + _lookaheadFirst.Value);
            }

        }
 

        private bool IsObject(DslToken token)
        {
            return token.TokenType == TokenType.Collection
                   || token.TokenType == TokenType.Feature
                   || token.TokenType == TokenType.Type;
        }

        private bool IsEqualityOperator(DslToken token)
        {
            return token.TokenType == TokenType.Equals
                   || token.TokenType == TokenType.NotEquals;
        }

    

        private void CreateNewMatchCondition()
        {
            _currentMatchCondition = new MatchCondition();
            _featureModel.Filters.Add(_currentMatchCondition);
        }
    }
}
