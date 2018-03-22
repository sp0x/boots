using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nvoid.db.Extensions;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex
{
    /// <summary>
    /// An expression visitor that generates JS
    /// </summary>
    public class JsGeneratingExpressionVisitor : ExpressionVisitor
    { 
        public Dictionary<VariableExpression, string> Variables { get; private set; }
        public JsGeneratingExpressionVisitor()
            : base()
        {
            Variables = new Dictionary<VariableExpression, string>();
        } 

        protected override string VisitNumberExpression(NumberExpression exp)
        {
            var output = exp.ToString();
            return output;
        }

        protected override string VisitStringExpression(StringExpression exp)
        {
            return exp.ToString();
        }

        protected override string VisitFloatExpression(FloatExpression exp)
        {
            return exp.ToString();
        }

        public override string VisitBinaryExpression(BinaryExpression exp)
        {
            var left = Visit(exp.Left);
            var right = Visit(exp.Right);
            var op = $"{left} {exp.Token.Value} {right}";
            return op;
        }
        protected override string VisitAssignment(AssignmentExpression exp)
        {
            var sb = new StringBuilder();
            sb.Append($"var {exp.Member}");
            sb.Append("=");
            var assignValue = Visit(exp.Value);
            sb.Append(assignValue);
            sb.Append(";");
            AddVariable(exp.Member, sb.ToString());
            return sb.ToString();
        }

        

        private void AddVariable(VariableExpression expMember, string expValue)
        {
            Variables.Add(expMember, expValue);
        }

        protected override string VisitFunctionCall(CallExpression exp, out object resultObj)
        {
            var function = exp.Name;
            var paramBuilder = new StringBuilder();
            var iParam = 0;
            foreach (var parameter in exp.Parameters)
            {
                var paramStr = Visit(parameter);
                paramBuilder.Append(paramStr);
                if (iParam < exp.Parameters.Count - 1)
                {
                    paramBuilder.Append(", ");
                }
                iParam++;
            }
            var jsFunction = JsFunctions.Resolve(function, exp.Parameters);
            resultObj = null;
            var result = $"{jsFunction}({paramBuilder})";
            return result;
        }

        protected override string VisitParameter(ParameterExpression exp)
        { 
            var sb = new StringBuilder();
            var paramValue = exp.Value;
            var paramValueType = paramValue.GetType();
            if (paramValueType == typeof(LambdaExpression))
            {
                var value = VisitLambda(paramValue as LambdaExpression);
                sb.Append(value);
            }
            else if (paramValueType == typeof(CallExpression))
            {
                object jsout;
                var value = VisitFunctionCall(paramValue as CallExpression, out jsout);
                sb.Append(value);
            }
            else
            {
                sb.Append(exp.ToString());
            }
            return sb.ToString();
        }

        private string VisitLambda(LambdaExpression lambda)
        {
            var sb = new StringBuilder();
            sb.Append("function(");
            for(int i=0; i<lambda.Parameters.Count; i++)
            {
                var param = lambda.Parameters[i];
                var strParam = Visit(param);
                sb.Append(strParam);
                if(i<(lambda.Parameters.Count - 1)) sb.Append(", ");
            }
            sb.Append("){\n");
            var bodyContent = Visit(lambda.Body.FirstOrDefault());
            sb.Append(" return ").Append(bodyContent).Append(";");
            sb.Append("\n}"); 
            return sb.ToString();
        }

        protected override string VisitVariableExpression(VariableExpression exp)
        {
            var val = exp.ToString();
            return val;
        }

        public override ExpressionVisitResult CollectVariables(IExpression root)
        {
            var strValue = Visit(root);
            var result = new ExpressionVisitResult();
            result.Value = strValue;
            result.Variables = Variables;
            return result;
        }
    }

    public class ExpressionVisitResult
    {
        public Dictionary<VariableExpression, string> Variables { get; set; }
        public string Value { get; set; }
    }
}