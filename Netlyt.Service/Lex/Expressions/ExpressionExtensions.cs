using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Lex.Expressions
{
    public static class ExpressionExtensions
    {
        public static string ConcatExpressions(this IEnumerable<IExpression> expressions)
        {
            if (expressions == null)
            {
                return "";
            }
            return string.Join(", ", expressions);
        }
    }
}
