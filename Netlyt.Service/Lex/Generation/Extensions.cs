using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex.Generation
{
    public static class Extensions
    {
        public static CodeGenerator GetCodeGenerator(this IExpression expression)
        {
            if (expression.GetType() == typeof(MapReduceExpression))
            {
                return new MapReduceGenerator();
            }
            else if (expression.GetType() == typeof(MapAggregateExpression))
            {
                return new MapAggregateGenerator();
            }
            else
            {
                throw new Exception("No generator for this expression!");
            }
        }
    }
}
