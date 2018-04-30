using System.Collections.Generic;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    public static class ExpressionExtensions
    {
        public static string ConcatExpressions(this IEnumerable<IExpression> expressions, string glue = ", ")
        {
            if (expressions == null)
            {
                return "";
            }
            return string.Join(glue, expressions);
        }
    }
}
