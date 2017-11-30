using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Lex.Expressions
{
    public static class ExpressionExtensions
    {
        public static string ConcatTokens(this IEnumerable<IExpression> expressions)
        {
            return string.Join(", ", expressions);
        }
    }
}
