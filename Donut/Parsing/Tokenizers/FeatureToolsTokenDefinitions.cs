using Donut.Parsing.Tokens;

namespace Donut.Parsing.Tokenizers
{
    public class FeatureToolsTokenDefinitions : TokenDefinitionCollection
    {
        public FeatureToolsTokenDefinitions()
        {
            TokenDefinitions.Add(new TokenDefinition(TokenType.And, "(^|\\W)and(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Or, "(^|\\W)or(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Not, "(^|\\W)not(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Between, "between", 1));
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
            TokenDefinitions.Add(new TokenDefinition(TokenType.NotIn, "not\\sin", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.In, "(^|\\W)in(?=[\\s\\t])", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.DateTimeValue, "\\d\\d\\d\\d-\\d\\d-\\d\\d \\d\\d:\\d\\d:\\d\\d", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.StringValue, "('|\")([^']*)('|\")", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.NumberValue, "(-?)\\d+", 2));
            TokenDefinitions.Add(new TokenDefinition(TokenType.FloatValue, "(-?)\\d+\\.\\d+", 2));
            TokenDefinitions.Add(new TokenDefinition(TokenType.First, "first_", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Last, "last_", 1));
            //TokenDefinitions.Add(new TokenDefinition(TokenType.Underscore, "_", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.DatasetTime, "_time", 1));
            TokenDefinitions.Add(new TokenDefinition(TokenType.Symbol, "[\\w\\d_]+", 3));
            //
        }
    }
}