using Donut.Parsing.Tokens;

namespace Donut.Parsing.Tokenizers
{
    public class DonutTokenDefinitions : TokenDefinitionCollection
    {

        public DonutTokenDefinitions()
        {
            TokenDefinitions.Add(new TokenDefinition(TokenType.And, "(^|\\W)and(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Or, "(^|\\W)or(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Not, "(^|\\W)not(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Between, "between", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Target, "target\\s[\\w-_\\d]{1,100}", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.OpenParenthesis, "\\(", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.OpenBracket, "\\[", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.OpenCurlyBracket, "\\{", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.CloseParenthesis, "\\)", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.CloseBracket, "\\]", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.CloseCurlyBracket, "\\}", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Comma, ",", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.MemberAccess, "\\.", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Lambda, "=>", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Assign, "=", 2));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Equals, "==", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.NotEquals, "!=", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Semicolon, ";", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.NewLine, "\n", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Add, "\\+", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Subtract, "-", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Multiply, "\\*", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Divide, "/", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Define, "define", 1)); //(?<=define\\s)([\\w\\d_]+)
            TokenDefinitions.Add(new TokenDefinition(TokenType.ReduceAggregate, "(^|\\W)reduce aggregate(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Reduce, "(^|\\W)reduce(?=[\\s\\t])", 2));
            TokenDefinitions.Add(new TokenDefinition(TokenType.ReduceMap, "(^|\\W)reduce_map(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.NotIn, "not\\sin", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.In, "(^|\\W)in(?=[\\s\\t])", 1));
            //TokenDefinitions.Add(new TokenDefinition(TokenType.Like, "like", 1));
            //TokenDefinitions.Add(new TokenDefinition(TokenType.Limit, "limit", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.From, "(^|\\W)from(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.OrderBy, "(^|\\W)order\\sby", 1));
            //TokenDefinitions.Add(new TokenDefinition(TokenType.Message, "msg|message", 1));
            //TokenDefinitions.Add(new TokenDefinition(TokenType.NotLike, "not like", 1)); 
            TokenDefinitions.Add(new TokenDefinition(TokenType.DateTimeValue, "\\d\\d\\d\\d-\\d\\d-\\d\\d \\d\\d:\\d\\d:\\d\\d", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.StringValue, "('|\")([^']*)('|\")", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.NumberValue, "(-?)\\d+", 2));
            TokenDefinitions.Add(new TokenDefinition(TokenType.FloatValue, "(-?)\\d+\\.\\d+", 2));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Set, "(^|\\s)(set)(?=[\\s\t])", 2));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Symbol, "[\\w\\d_]+", 3));
        }

    }
}