﻿namespace Netlyt.Service.Lex.Parsing.Tokens
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
        Lambda,
        Add, Subtract, Multiply, Divide,
        Comma,
        CloseParenthesis, OpenParenthesis,
        CloseBracket, OpenBracket,
        CloseCurlyBracket, OpenCurlyBracket,
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
        Symbol,
        Reduce,
        ReduceMap,
        ReduceAggregate,
        Semicolon,
        Target,
        NewLine
    }
}
