﻿using System;
using System.Collections.Generic;
using System.Text;
using Donut;
using Netlyt.Interfaces;

namespace Netlyt.Service.Lex.Expressions
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
