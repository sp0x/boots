using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Donut;
using Donut.Features;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Donut.Lex.Generators;
using MongoDB.Bson;
using Netlyt.Interfaces;

namespace Donut.Lex.Generators
{
    /// <summary>
    /// Helps with generating aggregation pipelines from a collection of features.
    /// </summary>
    public class AggregateFeatureCodeGenerator : FeatureCodeGenerator
    {
        private Donut.Data.DataIntegration _rootIntegration;
        private DonutFunctions _donutFnResolver;
        private DatasetMember _rootDataMember;
        private string _outputCollection;
        private AggregateFeatureGeneratingExpressionVisitor _expVisitor;
        private List<AggregateJobTree> _aggregateJobTrees;

        public bool HasProjection { get; private set; }
        public bool HasGroupingFields { get; private set; }
        public bool HasGroupingKeys { get; private set; }
        public bool HasFilters { get; private set; }

        public AggregateFeatureCodeGenerator(DonutScript script, AggregateFeatureGeneratingExpressionVisitor expVisitor)
            : base(script)
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

        private AggregateJobTree AddAggregateTreeFromField(NameExpression expression)
        {
            Clean();
            _expVisitor.Clear();
            var firstCallExpression = new CallExpression()
            {
                Name = "first"
            };
            firstCallExpression.AddParameter(new ParameterExpression(expression));
            var firstGrpStage = AddAggregateTreeFromCall(firstCallExpression);
            //var outputTree = _expVisitor.AggregateTree.Clone();
            return firstGrpStage;
        }
        /// <summary>
        /// Creates an aggregate tree from a call
        /// </summary>
        /// <param name="callExpression"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        private AggregateJobTree AddAggregateTreeFromCall(CallExpression callExpression, AssignmentExpression feature = null)
        {
            //            var isAggregate = fnDict.IsAggregate(callExpression);
            //            var functionType = fnDict.GetFunctionType(callExpression);
            Clean();
            _expVisitor.Clear();
            var strValues = VisitCall(callExpression, null, _expVisitor);
            var outputTree = _expVisitor.AggregateTree.Clone();
            return outputTree;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callExpression"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        public FeatureFunctionsCodeResult GenerateFeatureFunctionCall(CallExpression callExpression, AssignmentExpression feature = null)
        {
            var fnDict = new DonutFunctions();
            var isAggregate = fnDict.IsAggregate(callExpression);
            var functionType = fnDict.GetFunctionType(callExpression);
            Clean();
            _expVisitor.Clear();
            var strValues = VisitCall(callExpression, null, _expVisitor);
            if (string.IsNullOrEmpty(strValues))
            {
                return null;
            }
            if (isAggregate)
            {
                var aggregateField = new BsonDocument();
                if (feature == null)
                {
                    try
                    {
                        aggregateField = BsonDocument.Parse(strValues);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Failed to parse expression: {callExpression}\nError: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    aggregateField[feature.Member.ToString()] = BsonDocument.Parse(strValues);
                }
                var result = new FeatureFunctionsCodeResult(functionType, aggregateField.ToString());
                return result;
            }
            else
            {
                return new FeatureFunctionsCodeResult(strValues);
            }
        }

        /// <summary>
        /// Add a feature assignment to the aggregate pipeline
        /// </summary>
        /// <param name="feature"></param>
        public override void Add(AssignmentExpression feature)
        {
            IExpression fExpression = feature.Value;
            string fName = GetFeatureName(feature);//feature.Member.ToString();

            var featureFType = fExpression.GetType();
            string featureContent = null;
            if (featureFType == typeof(NameExpression))
            {
                var aggTree = AddAggregateTreeFromField(fExpression as NameExpression);
                aggTree.Name = fName;
                _aggregateJobTrees.Add(aggTree);
                //                var member = (fExpression as NameExpression).Member?.ToString();
                //                //In some cases we might just use the field
                //                if (string.IsNullOrEmpty(member)) member = fExpression.ToString();
                //                featureContent = $"groupFields[\"{fName}\"] = " + "new BsonDocument { { \"$first\", \"$" + member + "\" } };";
            }
            else if (fExpression.IsDonutAggregateFunction())
            {
                //We're dealing with an aggregate call 
                var aggregateTree = AddAggregateTreeFromCall(fExpression as CallExpression);
                aggregateTree.Name = fName;
                _aggregateJobTrees.Add(aggregateTree);
                var functionType = _donutFnResolver.GetFunctionType(fExpression as CallExpression);
                switch (functionType)
                {
                    case DonutFunctionType.GroupField:
                        HasGroupingFields = true;
                        break;
                    case DonutFunctionType.Project:
                        HasProjection = true;
                        break;
                    case DonutFunctionType.GroupKey:
                        HasGroupingKeys = true;
                        break;
                    case DonutFunctionType.Filter:
                        HasFilters = true;
                        break;
                }
            }
            else
            {
                //This is not a donut aggregate
                throw new NotImplementedException(fExpression.ToString());
            }
        }

        /// <summary>
        ///  
        /// </summary>
        /// <returns></returns>
        public string GetScriptContent()
        {
            var fBuilder = new StringBuilder();
            var aggGroups = new Dictionary<string, AggregateStage>();
            var sbGroups = new StringBuilder();
            var sbProjections = new StringBuilder();
            var groupHashes = new Dictionary<string, string>();
            foreach (var aggJobTree in _aggregateJobTrees)
            {
                foreach (var stage in aggJobTree.Stages)
                {
                    var groups = stage.GetGroupings().ToList();
                    var stageJson = stage.WrapValueWithRoot(aggJobTree.Name);
                    if (string.IsNullOrEmpty(stageJson))
                    {
                        continue;
                    }
                    int iGrp = 0;
                    foreach (var grp in groups)
                    {
                        var grpHash = grp.GetHashCode();
                        var mName = "g" + grpHash.ToString();
                        string groupJson;
                        if (!groupHashes.ContainsKey(grpHash))
                        {
                            groupJson = grp.WrapValue(false);
                            var groupWrapedRoot = grp.WrapValueWithRoot(mName);
                            var bsonDoc = $"groupFields.Merge(BsonDocument.Parse({groupWrapedRoot}));";
                            aggGroups[grp.Function.GetHashCode().ToString()] = stage;
                            sbGroups.AppendLine(bsonDoc);
                            groupHashes[grpHash] = groupJson;
                        }
                        else
                        {
                            groupJson = groupHashes[grpHash];
                        }
                        stageJson = stageJson.Replace(groupJson, $"\"\"${mName}\"\"");
                        iGrp++;
                    }
                    GenerateStageAggregateCode(aggJobTree.Name, stage, sbProjections, sbGroups, stageJson);
                }
            }
            fBuilder.AppendLine(sbGroups.ToString());
            fBuilder.AppendLine(sbProjections.ToString());
            var aggregatePipeline = GeneratePipeline();
            fBuilder.Append(aggregatePipeline);
            return fBuilder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The name of the field</param>
        /// <param name="stage">The stage that you're processing</param>
        /// <param name="sbProjections">A buffer of projections to append to</param>
        /// <param name="sbGroups">A buffer of group fields to append to</param>
        /// <param name="stageJson">The name wrapped json of a stage</param>
        private static void GenerateStageAggregateCode(string name, AggregateStage stage, StringBuilder sbProjections,
            StringBuilder sbGroups, string stageJson)
        {
            switch (stage.Type)
            {
                case AggregateStageType.Group:
                    //TODO: provide group name
                    var stageWrap = stage.WrapValueWithRoot(name);
                    var gBsonDoc = $"groupFields.Merge(BsonDocument.Parse({stageWrap}));";
                    //Append our group to the projections so that it's visible
                    var pBsonDocFake = $"projections.Merge(new BsonDocument" + "{{\"" + name + "\", \"$" +
                                       name + "\"}}" + ");";
                    sbProjections.AppendLine(pBsonDocFake);
                    sbGroups.AppendLine(gBsonDoc);
                    break;
                case AggregateStageType.Project:
                    var pBsonDoc = $"projections.Merge(BsonDocument.Parse({stageJson}));";
                    sbProjections.AppendLine(pBsonDoc);
                    break;
                default:
                    throw new NotImplementedException("Support for root pipeline expression not implemented: " +
                                                      stage.Type.ToString());
            }
        }

        public string GeneratePipeline()
        {
            var fBuilder = new StringBuilder();
            if (!HasGroupingKeys && HasGroupingFields)
            {
                var groupKey = "";
                var ignKeys = _rootIntegration.AggregateKeys;
                if (ignKeys != null || !ignKeys.Any())
                {
                    throw new Exception("Integration has no aggregate keys!");
                }
                var tsKey = _rootIntegration?.DataTimestampColumn;
                if (_rootIntegration != null && ignKeys != null)
                {
                    foreach (var key in ignKeys)
                    {
                        var content = key.ToString();
                        groupKey += $"groupKeys.Merge({content});\n";
                    }
                    groupKey += $"var grouping = new BsonDocument();\n" +
                                $"grouping[\"_id\"] = groupKeys;\n" +
                                $"grouping = grouping.Merge(groupFields);";
                }
                else
                {
                    groupKey = $"groupKeys[\"_id\"] = \"$_id\";\n";
                }
                fBuilder.AppendLine(groupKey);
            }

            if (HasGroupingFields || HasProjection)
            {
                if (HasGroupingFields)
                {
                    var groupStep = @"pipeline.Add(new BsonDocument{
                                        {" + "\"$group\", grouping}" +
                                    "});";
                    fBuilder.AppendLine(groupStep);
                }
                if (HasProjection)
                {
                    var projectStep = @"pipeline.Add(new BsonDocument{
                                        {" + "\"$project\", projections} " +
                                      "});";
                    fBuilder.AppendLine(projectStep);
                }
                var outputStep = @"pipeline.Add(new BsonDocument{
                                {" + "\"$out\", \"" + _outputCollection + "\"" + "}" +
                                 "});";
                fBuilder.AppendLine(outputStep);
                var record = "var aggOptions = new AggregateOptions(){ AllowDiskUse = true, BatchSize=1  };\n";
                record += $"var aggregateResult = rootCollection.Aggregate<BsonDocument>(pipeline, aggOptions);";
                fBuilder.AppendLine(record);
            }
            return fBuilder.ToString();
        }

        public override string GenerateFromExpression(Expression mapReduce)
        {
            throw new NotImplementedException();
        }

        public void Clean()
        {
        }
    }
}