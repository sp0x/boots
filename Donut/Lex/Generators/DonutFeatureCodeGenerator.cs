using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Donut.Features;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Netlyt.Interfaces;

namespace Donut.Lex.Generators
{
    public class DonutFeatureCodeGenerator : FeatureCodeGenerator
    {
        private DonutFunctions _donutFnResolver;
        private Donut.Data.DataIntegration _rootIntegration;
        private DatasetMember _rootDataMember;
        private string _outputCollection;
        private DonutFeatureGeneratingExpressionVisitor _expVisitor;
        private List<IDonutFunction> _functions;

        public DonutFeatureCodeGenerator(DonutScript script, DonutFeatureGeneratingExpressionVisitor expVisitor) : base(script)
        {
            _donutFnResolver = new DonutFunctions();
            _rootIntegration = script.Integrations.FirstOrDefault();
            if (_rootIntegration == null)
                throw new InvalidIntegrationException("Script has no integrations");
            if (_rootIntegration.Fields == null || _rootIntegration.Fields.Count == 0)
                throw new InvalidIntegrationException("Integration has no fields");
            _rootDataMember = script.GetDatasetMember(_rootIntegration.Name);
            _outputCollection = _rootIntegration.FeaturesCollection;
            if (string.IsNullOrEmpty(_outputCollection))
            {
                throw new InvalidOperationException("Root integration must have a features collection set.");
            }
            _expVisitor = expVisitor ?? throw new ArgumentNullException(nameof(expVisitor));
            _functions = new List<IDonutFunction>();
        }

        public override string GenerateFromExpression(Expression mapReduce)
        {
            throw new NotImplementedException();
        }

        public override void Add(AssignmentExpression feature)
        {
            IExpression fExpression = feature.Value;
            string fName = GetFeatureName(feature);//feature.Member.ToString();
            if (fExpression is CallExpression donutFnCall)
            {
                var newFn = AddFeatureFromFunctionCall(donutFnCall);
                _functions.Add(newFn);
            }
            else
            {
                throw new NotImplementedException($"Donut function expression implemented for: {feature.ToString()}");
            }
        }

        private IDonutFunction AddFeatureFromFunctionCall(CallExpression donutFnCall)
        {
            //            var isAggregate = fnDict.IsAggregate(callExpression);
            //            var functionType = fnDict.GetFunctionType(callExpression);
            Clean();
            _expVisitor.Clear();
            var strValues = VisitCall(donutFnCall, null, _expVisitor);
            var fn = _expVisitor.FeatureFunctions.FirstOrDefault();
            return fn;
        }


        public void Clean()
        {
        }


        public DonutCodeFeatureDefinition GetScriptContent(string donutfileContent)
        {
            var prepareBuff = new StringBuilder();
            var extractBuff = new StringBuilder();
            foreach (var fn in _functions)
            {
                if (fn is IDonutTemplateFunction<string>)
                {
                    var value = fn.GetValue();
                    prepareBuff.AppendLine(value);
                }
                else if(fn is IDonutTemplateFunction<DonutCodeFeatureDefinition> codeFn)
                {
                    var content = codeFn.Content as DonutCodeFeatureDefinition;
                    prepareBuff.AppendLine(content.PrepareScript);
                    extractBuff.AppendLine(content.ExtractionScript);
                }
            }

            var def = new DonutCodeFeatureDefinition()
            {
                PrepareScript = prepareBuff.ToString(),
                ExtractionScript = extractBuff.ToString()
            };
            return def;
        }
    }
}