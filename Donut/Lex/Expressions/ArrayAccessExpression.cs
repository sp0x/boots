using System.Collections.Generic;
using System.Text;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    public class ArrayAccessExpression
        : Expression
    {
        public IExpression Object { get; set; }
        public List<ParameterExpression> Parameters { get; set; }

        public ArrayAccessExpression()
        {
            Parameters = new List<ParameterExpression>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Object.ToString());
            sb.Append("[");
            sb.Append(Parameters.ConcatExpressions());
            sb.Append("]");
            return sb.ToString();
        }
    }
}
