using System;
using System.Collections.Generic;
using System.Text;
using Donut;
using Netlyt.Interfaces;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Expressions
{
    public class BinaryExpression
        : Expression
    {
        public IExpression Left { get; set; }
        public IExpression Right { get; set; }
        public DslToken Token { get; private set; }

        public BinaryExpression(DslToken token)
        {
            this.Token = token;
        }
        public override IEnumerable<IExpression> GetChildren()
        {
            yield return Left;
            yield return Right;
        }

        public override string ToString()
        {
            return $"{Left} {Token.Value} {Right}";
        }
    }
}
