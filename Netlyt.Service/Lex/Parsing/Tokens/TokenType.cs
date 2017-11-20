namespace Netlyt.Service.Lex.Parsing.Tokens
{
    public enum TokenType
    {
        NotDefined,
        Define,
        And, Or, Not,
        In,NotIn,
        Assign,
        MemberAccess,
        Equals, NotEquals,
        Add, Subtract, Multiply, Divide,
        Comma,
        CloseParenthesis, OpenParenthesis,
        StringValue,
        FloatValue,
        Collection, Feature, Type,
        NumberValue,
        DateTimeValue,
        Between,
        Invalid,
        EOF,
        From,
        OrderBy,
        Set,
        Symbol
    }
}
