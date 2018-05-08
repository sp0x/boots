using System.Collections.Generic;
using System.Linq;
using System.Text;
using Donut.Lex;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using MongoDB.Bson.Serialization.Serializers;
using Netlyt.Interfaces;

namespace Donut.Features
{
    public class DonutFeatureGeneratingExpressionVisitor : ExpressionVisitor
    {
        private IDonutScript _script;
        public Queue<IDonutFunction> FeatureFunctions { get; set; }
        private DonutFunctions _functionDict;

        public DonutFeatureGeneratingExpressionVisitor(IDonutScript ds)
        {
            _script = ds;
            FeatureFunctions = new Queue<IDonutFunction>();
            _functionDict = new DonutFunctions();
        }

        public void Clear()
        {
            //Aggregates.Clear();
            FeatureFunctions.Clear();
            //_aggTree.Clear();
        }

        protected override string VisitFunctionCall(CallExpression exp, out object resultObj)
        {
            Depth++;
            resultObj = null;
            var function = exp.Name;
            var paramBuilder = new StringBuilder();
            var iParam = 0;
            var donutFn = _functionDict.GetFunction(function);
            donutFn.Parameters = exp.Parameters;
            //string result;
            //var aggStage = _aggTree.AddFunction(donutFn);

            if (donutFn is IDonutTemplateFunction<DonutCodeFeatureDefinition> fnTemplate)
            {
                FeatureFunctions.Enqueue(donutFn);
                var codeContext = new DonutCodeContext(_script);
                var donutFeatureDefinition = fnTemplate.GetTemplate(exp, codeContext);
                //result = donutFeatureDefinition.ToString();
                donutFn.Content = donutFeatureDefinition;
                resultObj = donutFn;
            }
            else
            {
                FeatureFunctions.Enqueue(donutFn);
                foreach (var parameter in exp.Parameters)
                {
                    var paramStr = Visit(parameter as IExpression);
                    paramBuilder.Append(paramStr);
                    if (iParam < exp.Parameters.Count - 1)
                    {
                        paramBuilder.Append(", ");
                    }
                    iParam++;
                }
                //result = $"{donutFn}({paramBuilder})";
                resultObj = donutFn;
            } 
            Depth--;
            return "";
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

        protected override string VisitParameter(ParameterExpression exp, out object retObject)
        {
            var sb = new StringBuilder();
            var paramValue = exp.Value;
            var paramValueType = paramValue.GetType();
            if (paramValueType == typeof(LambdaExpression))
            {
                var value = VisitLambda(paramValue as LambdaExpression, out retObject);
                sb.Append(value);
            }
            else if (paramValueType == typeof(CallExpression))
            {
                var value = VisitFunctionCall(paramValue as CallExpression, out retObject);
                sb.Append(value);
            }
            else if(paramValue is NameExpression varEx)
            {
                if (IsRootIntegrationAccess(_script, varEx))
                {
                    retObject = null;
                    var valueKey = GetIdentifier(varEx);
                    var accessor = $"document[\"{valueKey}\"]";
                    sb.Append(accessor);
                }
                else
                {
                    retObject = null;
                    var value = GetIdentifier(varEx);
                    sb.Append(value);
                }
            }
            else
            {
                retObject = null;
                sb.Append(paramValue.ToString());
            }
            return sb.ToString();
        }

        private bool IsRootIntegrationAccess(IDonutScript script, NameExpression varEx)
        {
            var rootIgn = script.GetRootIntegration();
            return rootIgn .Name==varEx.Name;
        }

        /// <summary>
        /// Gets the identifier refered by this name
        /// </summary>
        /// <returns></returns>
        public string GetIdentifier(NameExpression var)
        {
            if (var.Member == null) return var.Name;
            IExpression subExpression = var.Member;
            CallExpression foundCallExpression = null;
            IExpression paramValue = null;
            object resultObj = null;
            while (subExpression != null)
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
                    else if (memberExp is NameExpression)
                    {
                        if ((subExpression as MemberExpression)?.ChildMember != null)
                        {
                            subExpression = (subExpression as MemberExpression)?.ChildMember;
                        }
                        else
                        {
                            paramValue = memberExp;
                            break;
                        }
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
                return value;
            }
            else
            {
                var vex = ((NameExpression)paramValue);
                var varValue = VisitVariableExpression(vex, out resultObj);
                return varValue;
            }
        }

        protected override string VisitVariableExpression(NameExpression vex, out object resultObj)
        {
            resultObj = null;
            var output = vex.ToString();
            if (vex.Member != null) output = vex.Member.ToString();
            var donutFn = new InternalDonutFunctionProxy(output, output);
            resultObj = donutFn;
            return output;
        }

        private string VisitLambda(LambdaExpression lambda, out object retObject)
        {
            var sb = new StringBuilder();
            sb.Append("function(");
            for (int i = 0; i < lambda.Parameters.Count; i++)
            {
                var param = lambda.Parameters[i];
                var strParam = Visit(param);
                sb.Append(strParam);
                if (i < (lambda.Parameters.Count - 1)) sb.Append(", ");
            }
            sb.Append("){\n");
            var bodyContent = Visit(lambda.Body.FirstOrDefault(), out retObject);
            sb.Append(" return ").Append(bodyContent).Append(";");
            sb.Append("\n}");
            return sb.ToString();
        }
    }
}