using System.Collections.Generic;
using System.Text;
using Donut;
using Netlyt.Interfaces;

namespace Netlyt.Service.Lex.Expressions
{
    public class BlockExpression
        : Expression
    {
        public List<IExpression> Expressions { get; set; }

        public BlockExpression()
        {
            Expressions = new List<IExpression>();
        }

        public override IEnumerable<IExpression> GetChildren()
        {
            return Expressions;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append(Expressions.ConcatExpressions("\n"));
            sb.Append("\n}");
            return sb.ToString();
        }
    }
}