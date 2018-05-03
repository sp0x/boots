using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Features;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Netlyt.Interfaces;

namespace Donut.Lex.Generators
{
    public class DonutFeatureCodeGenerator : FeatureCodeGenerator
    {
        private DonutFunctions _donutFnResolver;
        private DataIntegration _rootIntegration;
        private DatasetMember _rootDataMember;
        private string _outputCollection;
        private AggregateFeatureGeneratingExpressionVisitor _expVisitor;
        private List<AggregateJobTree> _aggregateJobTrees;

        public DonutFeatureCodeGenerator(DonutScript script, AggregateFeatureGeneratingExpressionVisitor expVisitor) : base(script)
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
            _aggregateJobTrees = new List<AggregateJobTree>();
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
                AddFeatureFromFunctionCall(donutFnCall);
            }
            else
            {
                throw new NotImplementedException($"Donut function expression implemented for: {feature.ToString()}");
            }
        }

        private void AddFeatureFromFunctionCall(CallExpression donutFnCall)
        {
            //            var isAggregate = fnDict.IsAggregate(callExpression);
            //            var functionType = fnDict.GetFunctionType(callExpression);
            Clean();
            _expVisitor.Clear();
            var strValues = VisitCall(donutFnCall, null, _expVisitor);
            var outputTree = _expVisitor.AggregateTree.Clone();
        }


        public void Clean()
        {
        }
    }
}