using System.Collections.Generic;
using Donut.Parsing.Tokens;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    public class BinaryExpression
        : Expression
    {
        public IExpression Left { get; set; }
        public IExpression Right { get; set; }
        public DslToken Token { get; private set; }

        public BinaryExpression()
        {

        }
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
