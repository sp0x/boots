using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Lex.Expressions
{
    public static class ExpressionExtensions
    {
        public static string ConcatExpressions(this IEnumerable<IExpression> expressions)
        {
            return string.Join(", ", expressions);
        }
    }
}
