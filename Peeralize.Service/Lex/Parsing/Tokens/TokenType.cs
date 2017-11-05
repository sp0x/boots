namespace Peeralize.Service.Lex.Parsing.Tokens
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
        Comma,
        CloseParenthesis, OpenParenthesis,
        StringValue,
        Collection, Feature, Type,
        NumberValue,
        DateTimeValue,
        Between,
        Invalid,
        SequenceTerminator,
        From,
        OrderBy,
        Symbol
    }
}
