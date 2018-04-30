using System.Collections.Generic;
using Donut.Parsing.Tokens;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    public class UnaryExpression 
        : Expression
    {
        public IExpression Operand { get; set; }
        public DslToken Token { get; set; }

        public UnaryExpression(DslToken token)
        {
            this.Token = token;
        }

        public override IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression> {Operand};
        }
    }
}
