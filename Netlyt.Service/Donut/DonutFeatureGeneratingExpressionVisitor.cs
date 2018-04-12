using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Service.Lex;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generators;

namespace Netlyt.Service.Donut
{
    public class DonutFeatureGeneratingExpressionVisitor : ExpressionVisitor
    { 
        public Dictionary<VariableExpression, string> Variables { get; private set; }
        private DonutFunctions _functionDict;
        private DonutScript _script;
        AggregateJobTree _aggTree;
        public Queue<IDonutFunction> Aggregates { get; set; }
        public Queue<IDonutFunction> FeatureFunctions { get; set; }
        public AggregateJobTree AggregateTree => _aggTree;

        public DonutFeatureGeneratingExpressionVisitor(DonutScript script) : base()
        {
            Variables = new Dictionary<VariableExpression, string>();
            Aggregates = new Queue<IDonutFunction>();
            FeatureFunctions = new Queue<IDonutFunction>();
            _functionDict = new DonutFunctions();
            _script = script;
            _aggTree = new AggregateJobTree(_script);
        }
        
        protected override string VisitFunctionCall(CallExpression exp, out object resultObj)
        {
            Depth++;
            var function = exp.Name;
            var paramBuilder = new StringBuilder();
            var iParam = 0;
            var donutFn = _functionDict.GetFunction(function);
            donutFn.Parameters = exp.Parameters;
            string result;
            var aggStage = _aggTree.AddFunction(donutFn);

            if (donutFn is IDonutTemplateFunction fnTemplate)
            {
                FeatureFunctions.Enqueue(donutFn);
                var codeContext = new DonutCodeContext(_script);
                result = fnTemplate.GetTemplate(exp, codeContext); 
                donutFn.Content = result;
                resultObj = donutFn;
            }
            else if (donutFn.IsAggregate)
            {
                var aggregateResult = donutFn.GetValue();
                if (aggregateResult == null)
                {
                    resultObj = null;
                    return "";
                }
                donutFn.Content = aggregateResult = aggStage.GetValue();//FillCallParameters(donutFn, exp.Parameters);
                resultObj = donutFn;
                Aggregates.Enqueue(donutFn);
                result = aggregateResult;
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
                result = $"{donutFn}({paramBuilder})";
                resultObj = donutFn;
            }
            Depth--;
            return result;
        }
        

        protected override string VisitNumberExpression(NumberExpression exp, out object resultObj)
        {
            resultObj = null;
            var output = exp.ToString();
            return output;
        }

        protected override string VisitStringExpression(StringExpression exp, out object resultObj)
        {
            resultObj = null;
            return exp.ToString();
        }

        protected override string VisitFloatExpression(FloatExpression exp, out object resultObj)
        {
            resultObj = null;
            return exp.ToString();
        }

        public override string VisitBinaryExpression(BinaryExpression exp, out object resultObj)
        {
            resultObj = null;
            var left = Visit(exp.Left);
            var right = Visit(exp.Right);
            var op = $"{left} {exp.Token.Value} {right}";
            return op;
        }
        protected override string VisitAssignment(AssignmentExpression exp, out object resultObj)
        {
            var sb = new StringBuilder();
            sb.Append($"var {exp.Member}");
            sb.Append("=");
            var assignValue = Visit(exp.Value, out resultObj);
            sb.Append(assignValue);
            sb.Append(";");
            //AddVariable(exp.Member, sb.ToString());
            return sb.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        protected override string VisitParameter(ParameterExpression exp, out object resultObj)
        {
            var sb = new StringBuilder();
            var paramValue = exp.Value;
            var paramValueType = paramValue.GetType();
            if (paramValueType == typeof(NumberExpression))
            {
                return VisitNumberExpression(paramValue as NumberExpression, out resultObj);
            }
            else if (paramValueType == typeof(StringExpression))
            {
                return VisitStringExpression(paramValue as StringExpression, out resultObj);
            }
            else if (paramValueType == typeof(LambdaExpression))
            {
                var value = VisitLambda(paramValue as LambdaExpression, out resultObj);
                sb.Append(value);
            }
            else if (paramValueType == typeof(CallExpression))
            {
                var value = VisitFunctionCall(paramValue as CallExpression, out resultObj);
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
                    var value = VisitFunctionCall(foundCallExpression, out resultObj);
                    sb.Append(value);
                }
                else
                {
                    var vex = ((VariableExpression)paramValue);
                    var varValue = VisitVariableExpression(vex, out resultObj);
                    sb.Append(varValue);
                }
            }
            else
            {
                sb.Append(exp.ToString());
                resultObj = null;
            }
            return sb.ToString();
        } 

        protected override string VisitVariableExpression(VariableExpression vex, out object resultObj)
        {
            resultObj = null;
            var output = vex.ToString();
            if (vex.Member != null) output = vex.Member.ToString();
            var donutFn = new InternalDonutFunctionProxy(output, output);
            resultObj = donutFn;
            string result;
            //Dont add variables as a stage
            //var aggStage = _aggTree.AddFunction(donutFn);
            return output;
        }

        private string VisitLambda(LambdaExpression lambda, out object resultObj)
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
            var bodyContent = Visit(lambda.Body.FirstOrDefault(), out resultObj);
            sb.Append(" return ").Append(bodyContent).Append(";");
            sb.Append("\n}");
            return sb.ToString();
        }

        public void Clear()
        {
            Aggregates.Clear();
            FeatureFunctions.Clear();
            _aggTree.Clear();
        }

        public void SetScript(DonutScript script)
        {
            _script = script;
        }
         
    }
}