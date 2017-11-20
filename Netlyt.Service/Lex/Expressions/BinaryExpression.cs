using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Parsing.Tokens;

namespace Netlyt.Service.Lex.Expressions
{
    public class BinaryExpression
        : IExpression
    {
        public IExpression Left { get; set; }
        public IExpression Right { get; set; }
        public DslToken Token { get; private set; }

        public BinaryExpression(DslToken token)
        {
            this.Token = token;
        }
        public IEnumerable<IExpression> GetChildren()
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
