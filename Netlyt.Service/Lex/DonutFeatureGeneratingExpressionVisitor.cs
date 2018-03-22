using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Service.Donut;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex
{
    public class DonutFeatureGeneratingExpressionVisitor : ExpressionVisitor
    { 
        public Dictionary<VariableExpression, string> Variables { get; private set; }
        private DonutFunctionParser _donutFunctionParser;
        private DonutScript _script;
        public Queue<DonutFunction> Aggregates { get; set; }
        public Queue<DonutFunction> FeatureFunctions { get; set; }

        public DonutFeatureGeneratingExpressionVisitor(DonutScript script) : base()
        {
            Variables = new Dictionary<VariableExpression, string>();
            Aggregates = new Queue<DonutFunction>();
            FeatureFunctions = new Queue<DonutFunction>();
            _donutFunctionParser = new DonutFunctionParser();
            _script = script;
        }
        protected override string VisitFunctionCall(CallExpression exp, out object resultObj)
        {
            var function = exp.Name;
            var paramBuilder = new StringBuilder();
            var iParam = 0;
            var donutFn = _donutFunctionParser.Resolve(function, exp.Parameters); 
            if (donutFn.IsAggregate)
            {
                var aggregateResult = donutFn.GetAggregateValue();
                if (aggregateResult == null)
                {
                    resultObj = null;
                    return "";
                }
                for(int i=0; i<exp.Parameters.Count; i++)
                {
                    var parameter = exp.Parameters[i];
                    object paramOutputObj;
                    var paramStr = Visit(parameter, out paramOutputObj);
                    aggregateResult = aggregateResult.Replace("{" + i + "}", paramStr);
                }
                donutFn.Content = aggregateResult;
                resultObj = donutFn;
                Aggregates.Enqueue(donutFn);
                return aggregateResult;
            }
            else
            {
                FeatureFunctions.Enqueue(donutFn);
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
                var result = $"{donutFn}({paramBuilder})";
                resultObj = donutFn;
                return result;
            }
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
            //AddVariable(exp.Member, sb.ToString());
            return sb.ToString();
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
                object donutFn;
                var value = VisitFunctionCall(paramValue as CallExpression, out donutFn);
                sb.Append(value);
            }
            else if (paramValueType == typeof(VariableExpression))
            {
                var subExpression = (paramValue as VariableExpression).Member;
                CallExpression foundCallExpression = null;
                while (subExpression!=null)
                {
                    var memberExp = subExpression.Parent;
                    if (memberExp == null) break;
                    var memberExpType = memberExp.GetType();
                    if (memberExpType == typeof(CallExpression))
                    {
                        foundCallExpression = memberExp as CallExpression;
                        break;
                    }
                    else
                    {
                        if (memberExpType == typeof(MemberExpression))
                        {
                            subExpression = memberExp as MemberExpression;
                        }
                        else
                        {
                            //sb.Append(exp.ToString());
                            break;
                        }
                    }
                }
                if (foundCallExpression != null)
                {
                    object donutFn;
                    var value = VisitFunctionCall(foundCallExpression, out donutFn);
                    sb.Append(value);
                }
                else
                {
                    sb.Append(exp.ToString());
                }
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
            sb.Append("(");
            for (int i = 0; i < lambda.Parameters.Count; i++)
            {
                var param = lambda.Parameters[i];
                var strParam = Visit(param);
                sb.Append(strParam);
                if (i < (lambda.Parameters.Count - 1)) sb.Append(", ");
            }
            sb.Append(")=>{\n");
            var bodyContent = Visit(lambda.Body.FirstOrDefault());
            sb.Append(" return ").Append(bodyContent).Append(";");
            sb.Append("\n}");
            return sb.ToString();
        }

        public void Clear()
        {
            Aggregates.Clear();
            FeatureFunctions.Clear();
        }

        public void SetScript(DonutScript script)
        {
            _script = script;
        }
    }
}