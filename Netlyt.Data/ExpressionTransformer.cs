using System;
using System.Linq.Expressions;

namespace Netlyt.Data
{
    public static class ExpressionTransformer<TFrom, TTo>
        where TFrom : TTo
    {


        public class Visitor : ExpressionVisitor
        {
            private ParameterExpression _parameter;

            public Visitor(ParameterExpression param)
            {
                _parameter = param;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return _parameter;
            }

        }


        public static Expression<Func<TTo, bool>> TranformPredicate(Expression<Func<TFrom, bool>> expression)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TTo));
            Expression body = new Visitor(parameter).Visit(expression.Body);
            return Expression.Lambda<Func<TTo, bool>>(body, parameter);
        }

        public static Expression<Func<T, TTo>> Transform<T>(Expression<Func<T, TFrom>>  exp)
        {
            //Create a parameter for TTo, to which we'll be converting
            ParameterExpression param = Expression.Parameter(typeof(TTo), "member");
            Expression body = new Visitor(param).Visit(exp.Body);
            return Expression.Lambda<Func<T, TTo>>(body, param);
        }



    }
}