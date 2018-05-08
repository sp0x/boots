using System;
using System.Collections.Generic;
using System.Text;
using Donut.Lex.Expressions;
using Netlyt.Interfaces;

namespace Donut.Lex.Data
{
    public static class Extensions
    {
        public static bool IsDonutAggregateFeature(this AssignmentExpression fExpr)
        {
            IExpression fExpression = fExpr.Value;
            if (fExpression.IsDonutAggregateFunction()) return true;
            if (fExpression is NameExpression varExpr)
            {
                return true;
            }
            return false;
        }

        public static bool IsDonutNativeFeature(this AssignmentExpression fExpr)
        {
            var fExpression = fExpr.Value;
            if (fExpression is CallExpression callExpr)
            {
                var df = new DonutFunctions();
                if (df.IsAggregate(callExpr)) return false;
                var fnType = df.GetFunctionType(callExpr);
                return fnType == DonutFunctionType.Donut;
            }
            return false;
        }
        public static bool IsDonutAggregateFunction(this IExpression expr)
        {
            if (expr is CallExpression callExpr)
            {
                var df = new DonutFunctions();
                return df.IsAggregate(callExpr);
            }
            return false;
        }
    }
}
