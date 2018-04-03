using System;
using System.Text;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generation;

namespace Netlyt.Service.Lex.Generators
{
    public class DonutAggregateGenerator : CodeGenerator
    {
        public DonutAggregateGenerator()
        {
        }
        public override string GenerateFromExpression(Expression mapReduce)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="callExpression"></param>
        /// <param name="expVisitor"></param>
        /// <returns></returns>
        public string ProcessCall(CallExpression callExpression, DonutFeatureGeneratingExpressionVisitor expVisitor)
        {
            //if(expVisitor==null) expVisitor = new DonutFeatureGeneratingExpressionVisitor(_script);
            var lstValues = VisitCall(callExpression, null, expVisitor);
            return lstValues;
        }

        public void Clean()
        {
        }
    }
}