using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Parsing
{
    public class Parser
    { 
        private DslFeatureModel _featureModel;
        private MatchCondition _currentMatchCondition;

        private const string ExpectedObjectErrorText = "Expected =, !=, IN or NOT IN but found: ";
        private OrderByExpression OrderBy { get; set; }

        private DslFeatureModel FeatureModel{
            get { return _featureModel; }
            set { _featureModel = value; }
        }
        private TokenReader Reader { get; set; }

        /// <summary>
        /// Loads tokens for parsing
        /// </summary>
        /// <param name="tokens"></param>
        public void Load(List<DslToken> tokens)
        {
            Reader = new TokenReader(tokens); 
        }

        public DslFeatureModel Parse(List<DslToken> tokens)
        {
            Load(tokens);
            FeatureModel = new DslFeatureModel();
            Define(); 
            //DiscardToken(TokenType.SequenceTerminator); 
            return _featureModel;
        }

        /// <summary>
        /// Reads the next available expression.
        /// </summary>
        /// <returns></returns>
        public IExpression ReadExpression()
        {
            return ReadExpressions(null, 1).FirstOrDefault();
        }
        /// <summary>
        /// Reads the next available expressions
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<IExpression> ReadExpressions(Predicate<TokenCursor> predicate = null, int limit = 0)
        {
            Stack<IExpression> previousExpressions = new Stack<IExpression>();
            Queue<IExpression> outputExpressions = new Queue<IExpression>();
            uint ix = 0;
            while (!Reader.IsComplete)
            {
                if (predicate!=null && predicate(Reader.Cursor)) break;
                if (limit != 0 && ix >= limit) break;

                var nextToken = Reader.Current;
                IExpression crExpression = null;
                switch (nextToken.TokenType)
                {
                    case TokenType.OrderBy:
                        crExpression = ReadOrderBy();
                        break;
                    case TokenType.Set:
                        crExpression = ReadVariableSet();
                        break;
                    case TokenType.Symbol:
                        if (IsFunctionCall(Reader.Current, Reader.NextToken))
                        {
                            var func = ReadFunction();
                            crExpression = func;
                        }
                        else if (IsVariableExpression(Reader.Current, Reader.NextToken))
                        {
                            var variable = ReadVariable();
                            crExpression = variable;
                        }
                        else
                        {
                            var member = ReadMemberChainExpression();
                            crExpression = member;
                        }
                        break;
                    case TokenType.NumberValue:
                        crExpression = ReadConstant();
                        break;
                    case TokenType.StringValue:
                        crExpression = ReadConstant();
                        break;
                    case TokenType.FloatValue:
                        crExpression = ReadConstant();
                        break;
                    case TokenType.Not:
                        var unaryOp = new UnaryExpression(nextToken);
                        Reader.DiscardToken();
                        unaryOp.Operand = ReadExpression();
                        crExpression = unaryOp;

                        break;
                    default:
                        if (IsOperator(nextToken))
                        {
                            var op = new BinaryExpression(nextToken);
                            Reader.DiscardToken();
                            var left = previousExpressions.Pop();
                            outputExpressions.Dequeue();
                            op.Left = left;
                            op.Right = ReadExpression();
                            crExpression = op;
                        }
                        else
                        {
                            throw new Exception($"Unexpected token at {nextToken.Line}:{nextToken.Position}:\n " +
                                                $"{nextToken}");
                        }
                        break;
                }
                outputExpressions.Enqueue(crExpression);
                previousExpressions.Push(crExpression);
                ix++;
                //yield return crExpression; 
            }
            return outputExpressions;
        }
         
        private IExpression ReadConstant()
        {
            var token = Reader.DiscardToken();
            IExpression output = null;
            switch (token.TokenType)
            {
                case TokenType.NumberValue:
                    output = new NumberExpression() { Value = int.Parse(token.Value)};
                    break;
                case TokenType.StringValue:
                    output = new StringExpression() {Value = token.Value};
                    break;
                case TokenType.FloatValue:
                    output = new FloatExpression() {Value = float.Parse(token.Value)};
                    break;
                default:
                    throw new Exception("Unexpected token for constant!");
                    break;
            }
            return output;
        }

        private bool IsOperator(DslToken token)
        {
            var tt = token.TokenType;
            return tt == TokenType.Add
                   || tt == TokenType.Subtract
                   || tt == TokenType.Multiply
                   || tt == TokenType.Divide
                   || tt == TokenType.Equals
                   || tt == TokenType.NotEquals
                   || tt == TokenType.In
                   || tt == TokenType.NotIn;
        }
        private ExpressionNode ReadExpressionData()
        {
            var tree = new ExpressionNode(null);
            short openParenthesis = 0;
            short closingParenthesis = 0;
            short expDepth = 0;
            ExpressionNode currentNode = null;
            while (true)
            {
                var isNewExp = false;
                //
                if (IsKeyword(Reader.Current) && expDepth == 0)
                {
                    break;
                }
                var token = Reader.DiscardToken();
                var nextToken = Reader.Current;
                var tokenNode = new ExpressionNode(token);
                tokenNode.SetDepth(expDepth);
                var isParenthesis = token.TokenType == TokenType.OpenParenthesis
                                    || token.TokenType == TokenType.CloseParenthesis;

                if (token.TokenType == TokenType.OpenParenthesis)
                {
                    currentNode = tokenNode;
                    expDepth++;
                }
                else if (token.TokenType == TokenType.CloseParenthesis)
                {
                    tree.AddChild(currentNode);
                    expDepth--;
                }
                else if (IsFunctionCall(token, nextToken))
                {
                    currentNode = tokenNode;
                    expDepth++;
                    var functionArgs = ReadParenthesisContent();
                    //Create a node of the function name -> function arguments
                }
                else if (expDepth == 0 && !isParenthesis)
                {
                    currentNode = tokenNode;
                }
                else
                {
                    currentNode.AddChild(token);
                }
            }
            if (Reader.Current.TokenType == TokenType.OpenParenthesis)
            {
                Reader.DiscardToken(TokenType.OpenParenthesis);
                //Read all untill closing parenthesis
            }
            return tree;
        }


        private void Define()
        {
            Reader.DiscardToken(TokenType.Define);
            var newSymbolName = Reader.ReadToken(TokenType.Symbol);
            _featureModel.Type = new FeatureTypeModel()
            {
                Name = newSymbolName.Value
            };
            Reader.DiscardToken(TokenType.Symbol);
            ReadFrom();
            ReadExpressions();
//            _featureModel.DateRange = new DateRange();
//            _featureModel.DateRange.From = DateTime.ParseExact(ReadToken(TokenType.DateTimeValue).Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public VariableExpression ReadVariable()
        {
            VariableExpression exp = null;
            string postfix = "";
            //Read the variable
            var token = Reader.DiscardToken(TokenType.Symbol);
            if (Reader.Current.TokenType == TokenType.MemberAccess)
            {
                var memberChain = ReadMemberChainExpression();
                postfix = "." + memberChain.ToString();
            }
            exp = new VariableExpression(token.Value + postfix);
            return exp;
        }

        public bool IsVariableExpression(DslToken tkA, DslToken tkB)
        {
            return tkA.TokenType == TokenType.Symbol 
                && (tkB.TokenType != TokenType.OpenParenthesis);
        }

        private MemberExpression ReadMemberChainExpression()
        {
            if (Reader.Current.TokenType != TokenType.MemberAccess) return null;
            MemberExpression rootMember = null;
            MemberExpression previousMember = null;
            while (true)
            {
                if (Reader.Current.TokenType != TokenType.MemberAccess) break;
                var member = ReadMemberExpression();
                if (rootMember == null)
                {
                    rootMember = member; 
                }
                else
                {
                    previousMember.SubElement = member;
                }
                previousMember = member;
            }
            return rootMember;
        }

        private MemberExpression ReadMemberExpression()
        {
            Reader.DiscardToken(TokenType.MemberAccess);
            var memberSymbol = Reader.DiscardToken(TokenType.Symbol);
            MemberExpression member = new MemberExpression(memberSymbol.Value);
            return member;

            //ExpressionNode currentNode = null;
            //            while (true)
            //            {
            //                //if a dont is here, go a level deeper..
            //                var token = Reader.Current;
            //                
            //                if ((token.TokenType != TokenType.Symbol && token.TokenType != TokenType.MemberAccess)
            //                    || (cursorPredicate!=null && cursorPredicate(Reader.Cursor)))
            //                {
            //                    break;
            //                }
            //                switch (token.TokenType)
            //                {
            //                    case TokenType.MemberAccess:
            //                        Reader.DiscardToken();
            //                        break;
            //                    case TokenType.Symbol:
            //                        var newNode = new ExpressionNode(Reader.DiscardToken());
            //                        if (currentNode != null) currentNode.AddChild(newNode);
            //                        else
            //                        {
            //                            currentNode = newNode;
            //                        }
            //                        break;
            //                } 
            //                //read symbol
            //
            //
            //            }
            //            return tree;
        }

        private AssignmentExpression ReadVariableSet()
        {
            Reader.DiscardToken(TokenType.Set);
            var typeNameToken = Reader.ReadToken(TokenType.Symbol);
            typeNameToken.Value = typeNameToken.Value.Trim();
            if (FeatureModel.Type.Name != typeNameToken.Value)
            {
                throw new Exception($"Unexpected Feature object at {typeNameToken.Line}:{typeNameToken.Position}!");
            }
            Reader.DiscardToken(TokenType.Symbol);
            Reader.DiscardToken(TokenType.MemberAccess);
            var memberAccessTree = ReadMemberExpression();
            //if (memberAccessTree.GetChildrenCount() > 1) throw new Exception("Invalid member assignment");
            ExpressionNode typedTree = null;
            var rootNode = new ExpressionNode(typeNameToken);
            var expression = memberAccessTree.GetChildren().First();
            rootNode.AddChild(expression);
            typedTree.AddChild(rootNode);

            Reader.DiscardToken(TokenType.Assign);
            var variableValueExpression = ReadExpressions().FirstOrDefault();

            var setExp = new AssignmentExpression(typedTree, variableValueExpression);
            FeatureModel.Features.Add(setExp);
            return setExp;
        }

        private OrderByExpression ReadOrderBy()
        {
            Reader.DiscardToken(TokenType.OrderBy);
            var tree = ReadExpressions();
            var expt = new List<IExpression>();
            expt.AddRange(tree);
            var orderBy = this.OrderBy = new OrderByExpression(expt);
            return orderBy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public FunctionExpression ReadFunction()
        {
            var f = new FunctionExpression();
            var tknFunctionName = Reader.DiscardToken(TokenType.Symbol);
            Reader.DiscardToken(TokenType.OpenParenthesis);
            f.Name = tknFunctionName.Value;
            var cursor = Reader.Cursor.Clone();
            //Create a predicate untill the closing of the function
            var fnEndPredicate = Parser.FunctionCallEnd(cursor);
            while (!Reader.IsComplete && !fnEndPredicate(Reader.Cursor))
            {
                ParameterExpression fnParameter = ReadFunctionParameter();
                f.AddParameter(fnParameter);
                if (Reader.Current.TokenType == TokenType.Comma)
                {
                    Reader.DiscardToken();
                }
            }
            Reader.DiscardToken(TokenType.CloseParenthesis);
            return f;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ParameterExpression ReadFunctionParameter()
        {
            var currentCursor = Reader.Cursor.Clone();
            //Look for a comma on the same level
            var paramValue = ReadExpressions(Parser.FunctionParameterEnd(currentCursor)).FirstOrDefault();
            if (paramValue == null) return null;
            var param = new ParameterExpression(paramValue); 
            return param;
        }
        

        public ExpressionNode ReadParenthesisContent()
        {
            ExpressionNode tree = null;
            Reader.DiscardToken(TokenType.OpenParenthesis);
            short expDepth = 0;
            ExpressionNode currentNode = null;
            while (true)
            {
                var token = Reader.DiscardToken();
                var nextToken = Reader.Current;
                var tokenNode = new ExpressionNode(token);
                tokenNode.SetDepth(expDepth);

                var isParenthesis = token.TokenType == TokenType.OpenParenthesis
                                    || token.TokenType == TokenType.CloseParenthesis;
                if (token.TokenType == TokenType.OpenParenthesis)
                {
                    currentNode = tokenNode;
                    expDepth++;
                }
                else if (token.TokenType == TokenType.CloseParenthesis)
                {
                    tree.AddChild(currentNode);
                    expDepth--;
                }
            }
            return tree;
        }

        /// <summary>
        /// Wether the two tokens form a function call
        /// </summary>
        /// <param name="tokenA"></param>
        /// <param name="tokenB"></param>
        /// <returns></returns>
        private bool IsFunctionCall(DslToken tokenA, DslToken tokenB)
        {
            return tokenA.TokenType == TokenType.Symbol && tokenB.TokenType == TokenType.OpenParenthesis;
        }



        private void ReadSymbolMemberAccess()
        {
            var symbol = Reader.ReadToken(TokenType.Symbol);
            Reader.DiscardToken(TokenType.Symbol);
            Reader.DiscardToken(TokenType.MemberAccess);
            var memberTokenStack = new Stack<DslToken>();
            while (true)
            {
                var memberToken = Reader.ReadToken(TokenType.Symbol);
                Reader.DiscardToken(TokenType.Symbol);
                memberTokenStack.Push(memberToken);
                if (Reader.Current.TokenType != TokenType.MemberAccess)
                {
                    break;
                }
            }
            memberTokenStack = memberTokenStack;
        } 

        /// <summary>
        /// Reads from tokens
        /// </summary>
        private List<string> ReadFrom()
        {
            Reader.DiscardToken(TokenType.From);
            var fromCollections = new List<string>();
            do
            {
                var symbol = Reader.ReadToken(TokenType.Symbol);
                Reader.DiscardToken(TokenType.Symbol);
                fromCollections.Add(symbol.Value);
                if (Reader.Current.TokenType != TokenType.Comma) break;
            } while (true);
            return fromCollections;
        }

//        private void MatchCondition()
//        {
//            CreateNewMatchCondition();
//
//            if (IsObject(Reader.Current))
//            {
//                if (IsEqualityOperator(_lookaheadSecond))
//                {
//                    EqualityMatchCondition();
//                }
//                else if (_lookaheadSecond.TokenType == TokenType.In)
//                {
//                    InCondition();
//                }
//                else if (_lookaheadSecond.TokenType == TokenType.NotIn)
//                {
//                    NotInCondition();
//                }
//                else
//                {
//                    throw new DslParserException(ExpectedObjectErrorText + " " + _lookaheadSecond.Value);
//                }
//
//                MatchConditionNext();
//            }
//            else
//            {
//                throw new DslParserException(ExpectedObjectErrorText + _lookaheadFirst.Value);
//            }
//        }

        private void EqualityMatchCondition()
        {
            _currentMatchCondition.Object = Reader.GetObject();
            Reader.DiscardToken();
            _currentMatchCondition.Operator = Reader.GetOperator();
            Reader.DiscardToken();
            _currentMatchCondition.Value = Reader.Current.Value;
            Reader.DiscardToken();
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
            _currentMatchCondition.Object = Reader.GetObject();
            Reader.DiscardToken();

            if (inOperator == DslOperator.In)
                Reader.DiscardToken(TokenType.In);
            else if (inOperator == DslOperator.NotIn)
                Reader.DiscardToken(TokenType.NotIn);

            Reader.DiscardToken(TokenType.OpenParenthesis);
            StringLiteralList();
            Reader.DiscardToken(TokenType.CloseParenthesis);
        }

        private void StringLiteralList()
        {
            _currentMatchCondition.Values.Add(Reader.ReadToken(TokenType.StringValue).Value);
            Reader.DiscardToken(TokenType.StringValue);
            StringLiteralListNext();
        }

        private void StringLiteralListNext()
        {
            if (Reader.Current.TokenType == TokenType.Comma)
            {
                Reader.DiscardToken(TokenType.Comma);
                _currentMatchCondition.Values.Add(Reader.ReadToken(TokenType.StringValue).Value);
                Reader.DiscardToken(TokenType.StringValue);
                StringLiteralListNext();
            }
            else
            {
                // nothing
            }
        }
         

        private bool IsNewExpressionStart(DslToken token)
        {
            switch (token.TokenType)
            {
                case TokenType.OpenParenthesis:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsObject(DslToken token)
        {
            return token.TokenType == TokenType.Collection
                   || token.TokenType == TokenType.Feature
                   || token.TokenType == TokenType.Type;
        }

        private bool IsKeyword(DslToken token)
        {
            return token.TokenType == TokenType.OrderBy
                   || token.TokenType == TokenType.Set;
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

        public static Predicate<TokenCursor> FunctionCallEnd(TokenCursor currentCursor)
        {
            return x =>
            {
                return (x.Depth) == currentCursor.Depth
                       && (x.Token.TokenType == TokenType.CloseParenthesis);
            };
        }

        public static Predicate<TokenCursor> FunctionParameterEnd(TokenCursor currentCursor)
        {
            return x =>
            {
                return x.Depth == currentCursor.Depth
                       && (x.Token.TokenType == TokenType.Comma || x.Token.TokenType == TokenType.CloseParenthesis);

            };
        }
    }
}
