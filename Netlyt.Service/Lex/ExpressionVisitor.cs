using System;
using System.Collections.Generic;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex
{
    /// <summary>
    /// A base expression visiter that supports visiting all expression types.
    /// </summary>
    public class ExpressionVisitor
    {
        public ExpressionVisitor()
        { 
        }

        public virtual ExpressionVisitResult CollectVariables(IExpression root)
        {
            throw new Exception("stub");
            //return new ExpressionVisitResult();
        }

        public virtual string Visit(IExpression expression)
        {
            object outputObj;
            return Visit(expression, out outputObj);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="visitObjResult">The resulting object from the visit</param>
        /// <returns></returns>
        public virtual string Visit(IExpression expression, out object visitObjResult)
        {
            var expType = expression.GetType();
            visitObjResult = null;
            if (expType == typeof(ParameterExpression))
            {
                return VisitParameter(expression as ParameterExpression);
            }
            else if (expType == typeof(AssignmentExpression))
            {
                return VisitAssignment(expression as AssignmentExpression);
            }
            else if (expType == typeof(CallExpression))
            {
                return VisitFunctionCall(expression as CallExpression, out visitObjResult);
            }
            else if (expType == typeof(BinaryExpression))
            {
                return VisitBinaryExpression(expression as BinaryExpression);
            }
            else if (expType == typeof(NumberExpression))
            {
                return VisitNumberExpression(expression as NumberExpression);
            }
            else if (expType == typeof(VariableExpression))
            {
                return VisitVariableExpression(expression as VariableExpression);
            }
            else
            {
                throw new Exception("Unsupported expression!");
            }
            
        }

        protected virtual string VisitVariableExpression(VariableExpression exp)
        {
            return null;
        }

        protected virtual string VisitNumberExpression(NumberExpression exp)
        {
            return null;    
        }

        protected virtual string VisitParameter(ParameterExpression parameterExpression)
        {
            return null;
        }

        protected virtual string VisitFunctionCall(CallExpression exp, out object resultObj)
        {
            resultObj = null;
            return null;
        }

        protected virtual string VisitAssignment(AssignmentExpression exp)
        { 
            return null;
        }

        public virtual string VisitBinaryExpression(BinaryExpression exp)
        {
            return null;
        }


        protected virtual string VisitStringExpression(StringExpression exp)
        {
            return null;
        }

        protected virtual string VisitFloatExpression(FloatExpression exp)
        {
            return null;
        }
         
    }
}
