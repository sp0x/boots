﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generation;

namespace Netlyt.Service.Lex.Generators
{
    /// <summary>
    /// Helps with generating aggregation pipelines from a collection of features.
    /// </summary>
    public class FeatureAggregateCodeGenerator : CodeGenerator
    {
        private DonutScript _script;
        private DataIntegration _rootIntegration;
        private DonutFunctions _donutFnResolver;
        private DatasetMember _rootDataMember;
        private string _outputCollection;
        private DonutFeatureGeneratingExpressionVisitor _expVisitor;
        private List<AggregateJobTree> _aggregateJobTrees;

        public bool HasProjection { get; private set; }
        public bool HasGroupingFields { get; private set; }
        public bool HasGroupingKeys { get; private set; }
        public bool HasFilters { get; private set; }

        public FeatureAggregateCodeGenerator(DonutScript script, DonutFeatureGeneratingExpressionVisitor expVisitor)
        {
            _script = script;
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
            _expVisitor = expVisitor;
            _aggregateJobTrees = new List<AggregateJobTree>();
        }
        /// <summary>
        /// Creates an aggregate tree from a call
        /// </summary>
        /// <param name="callExpression"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        private AggregateJobTree AddAggregateTreeFromCall(CallExpression callExpression, AssignmentExpression feature = null)
        {
            var fnDict = new DonutFunctions();
            //            var isAggregate = fnDict.IsAggregate(callExpression);
            //            var functionType = fnDict.GetFunctionType(callExpression);
            Clean();
            _expVisitor.Clear();
            var strValues = VisitCall(callExpression, null, _expVisitor);
            var outputTree = _expVisitor.AggregateTree.Clone();
            return outputTree;
            //            if (string.IsNullOrEmpty(strValues))
            //            {
            //                return null;
            //            }
            //            if (isAggregate)
            //            {
            //                var aggregateField = new BsonDocument();
            //                if (feature == null)
            //                {
            //                    try
            //                    {
            //                        aggregateField = BsonDocument.Parse(strValues);
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Trace.WriteLine($"Failed to parse expression: {callExpression}\nError: {ex.Message}");
            //                        return null;
            //                    }
            //                }
            //                else
            //                {
            //                    aggregateField[feature.Member.ToString()] = BsonDocument.Parse(strValues);
            //                }
            //                var result = new FeatureFunctionsCodeResult(functionType, aggregateField.ToString());
            //                return result;
            //            }
            //            else
            //            {
            //                return new FeatureFunctionsCodeResult(strValues);
            //            }
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



        public void AddAll(IEnumerable<AssignmentExpression> featureAssignments)
        {
            foreach (var f in featureAssignments)
            {
                Add(f);
            }
        }

        private string GetFeatureName(AssignmentExpression feature)
        {
            string fName = feature.Member.ToString();
            IExpression fExpression = feature.Value;
            var featureFType = fExpression.GetType();
            if (featureFType == typeof(VariableExpression))
            {
                var member = (fExpression as VariableExpression).Member?.ToString();
                //In some cases we might just use the field
                if (string.IsNullOrEmpty(member)) member = fExpression.ToString();
                if (member == _script.TargetAttribute)
                {
                    fName = member;
                }
            }
            return fName;
        }

        /// <summary>
        /// Add a feature assignment to the aggregate pipeline
        /// </summary>
        /// <param name="feature"></param>
        public void Add(AssignmentExpression feature)
        {
            IExpression fExpression = feature.Value;
            string fName = GetFeatureName(feature);//feature.Member.ToString();

            var featureFType = fExpression.GetType();
            string featureContent = null;
            if (featureFType == typeof(VariableExpression))
            {
                var member = (fExpression as VariableExpression).Member?.ToString();
                //In some cases we might just use the field
                if (string.IsNullOrEmpty(member)) member = fExpression.ToString();
                featureContent = $"groupFields[\"{fName}\"] = " + "new BsonDocument { { \"$first\", \"$" + member + "\" } };";
            }
            else if (featureFType == typeof(CallExpression))
            {
                if (_donutFnResolver.IsAggregate(fExpression as CallExpression))
                {
                    //We're dealing with an aggregate call 
                    var aggregateTree = AddAggregateTreeFromCall(fExpression as CallExpression);
                    aggregateTree.Name = fName;
                    _aggregateJobTrees.Add(aggregateTree);
                    var functionType = _donutFnResolver.GetFunctionType(fExpression as CallExpression);  
                    switch (functionType)
                    {
                        case DonutFunctionType.Group:
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

                    //TOADD
                            //                    var functionType = _donutFnResolver.GetFunctionType(fExpression as CallExpression);
                            //                    var aggregateValue = aggregateContent?.GetValue().Replace("$" + _rootIntegration.Name + ".", "$");
                            //                    if (aggregateValue != null) aggregateValue = aggregateValue.Replace("\"", "\\\"");
                            //                    switch (functionType)
                            //                    {
                            //                        case DonutFunctionType.Group:
                            //                            featureContent = $"groupFields[\"{fName}\"] = BsonDocument.Parse(\"{aggregateValue}\");";
                            //                            HasGroupingFields = true;
                            //                            break;
                            //                        case DonutFunctionType.Project:
                            //                            featureContent = $"projections[\"{fName}\"] = \"{aggregateValue}\";";
                            //                            HasProjection = true;
                            //                            break;
                            //                        case DonutFunctionType.GroupKey:
                            //                            if (!string.IsNullOrEmpty(aggregateValue))
                            //                            {
                            //                                featureContent = $"groupKeys[\"{fName}\"] = \"{aggregateValue}\";";
                            //                                HasGroupingKeys = true;
                            //                            }
                            //                            break;
                            //                        case DonutFunctionType.Standard:
                            //                            var variableName = GetFeatureVariableName(feature);
                            //                            if (!string.IsNullOrEmpty(variableName))
                            //                            {
                            //                                var fieldInfo = _rootIntegration.Fields?.FirstOrDefault(x => x.Name == variableName);
                            //                                var dttype = typeof(DateTime);
                            //                                if (fieldInfo.Type == dttype.FullName)
                            //                                {
                            //                                    featureContent = $"groupFields[\"{fName}\"] = new BsonDocument" + "{{ \"$first\", " +
                            //                                                     "new BsonDocument { { \"$dayOfYear\" , \"$" + variableName + "\" } }" +
                            //                                                     " }};";
                            //                                }
                            //                                else
                            //                                {
                            //                                    featureContent = $"groupFields[\"{fName}\"] = new BsonDocument" + "{{ \"$first\", \"$" + variableName + "\" }};";
                            //                                }
                            //
                            //                            }
                            //                            break;
                            //                    }
                    }
                else
                {
                    //We're dealing with a function call 
                    featureContent = featureContent;
                    //featureContent = GenerateFeatureFunctionCall(accessor as CallExpression, feature).Content;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Parses a feature assignment expression, to a string.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public string AddAndParse(AssignmentExpression feature)
        {
            IExpression fExpression = feature.Value;
            string fName = feature.Member.ToString();

            var featureFType = fExpression.GetType();
            string featureContent = null;
            if (featureFType == typeof(VariableExpression))
            {
                var member = (fExpression as VariableExpression).Member?.ToString();
                //In some cases we might just use the field
                if (string.IsNullOrEmpty(member)) member = fExpression.ToString();
                if (member == _script.TargetAttribute)
                {
                    fName = member;
                }
                featureContent = $"groupFields[\"{fName}\"] = " + "new BsonDocument { { \"$first\", \"$" + member + "\" } };";
            }
            else if (featureFType == typeof(CallExpression))
            {
                if (_donutFnResolver.IsAggregate(fExpression as CallExpression))
                {
                    //We're dealing with an aggregate call 
                    var aggregateContent = GenerateFeatureFunctionCall(fExpression as CallExpression);
                    var functionType = _donutFnResolver.GetFunctionType(fExpression as CallExpression);
                    var aggregateValue = aggregateContent?.GetValue().Replace("$" + _rootIntegration.Name + ".", "$");
                    if (aggregateValue != null) aggregateValue = aggregateValue.Replace("\"", "\\\"");
                    switch (functionType)
                    {
                        case DonutFunctionType.Group:
                            featureContent = $"groupFields[\"{fName}\"] = BsonDocument.Parse(\"{aggregateValue}\");";
                            HasGroupingFields = true;
                            break;
                        case DonutFunctionType.Project:
                            featureContent = $"projections[\"{fName}\"] = \"{aggregateValue}\";";
                            HasProjection = true;
                            break;
                        case DonutFunctionType.GroupKey:
                            if (!string.IsNullOrEmpty(aggregateValue))
                            {
                                featureContent = $"groupKeys[\"{fName}\"] = \"{aggregateValue}\";";
                                HasGroupingKeys = true;
                            }
                            break;
                        case DonutFunctionType.Standard:
                            var variableName = GetFeatureVariableName(feature);
                            if (!string.IsNullOrEmpty(variableName))
                            {
                                var fieldInfo = _rootIntegration.Fields?.FirstOrDefault(x => x.Name == variableName);
                                var dttype = typeof(DateTime);
                                if (fieldInfo.Type == dttype.FullName)
                                {
                                    featureContent = $"groupFields[\"{fName}\"] = new BsonDocument" + "{{ \"$first\", " +
                                                     "new BsonDocument { { \"$dayOfYear\" , \"$" + variableName + "\" } }" +
                                                     " }};";
                                }
                                else
                                {
                                    featureContent = $"groupFields[\"{fName}\"] = new BsonDocument" + "{{ \"$first\", \"$" + variableName + "\" }};";
                                }

                            }
                            break;
                    }
                }
                else
                {
                    //We're dealing with a function call 
                    featureContent = featureContent;
                    //featureContent = GenerateFeatureFunctionCall(accessor as CallExpression, feature).Content;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return featureContent;
        }

        /// <summary>
        /// Gets the name of the field from feature assignment.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private string GetFeatureVariableName(AssignmentExpression feature)
        {
            var seed = feature.Value;
            var itemQueue = new Queue<IExpression>();
            itemQueue.Enqueue(seed);
            while (itemQueue.Count > 0)
            {
                var item = itemQueue.Dequeue();
                var memberInfo = item.GetType();
                if (memberInfo == typeof(CallExpression))
                {
                    var subItems = (item as CallExpression).Parameters;
                    foreach (var param in subItems) itemQueue.Enqueue(param);
                }
                else if (memberInfo == typeof(VariableExpression))
                {
                    var mInfo = (item as VariableExpression).Member;
                    if (mInfo != null && mInfo.Parent != null && mInfo.Parent.GetType() == typeof(CallExpression))
                    {
                        var callExpParams = (mInfo.Parent as CallExpression).Parameters;
                        foreach (var param in callExpParams) itemQueue.Enqueue(param);
                    }
                    else
                    {
                        var member = mInfo?.ToString();
                        string memberName = !string.IsNullOrEmpty(member) ? member : (item as VariableExpression).Name;
                        return memberName;
                    }
                }
                else if (memberInfo == typeof(ParameterExpression))
                {
                    itemQueue.Enqueue((item as ParameterExpression).Value);
                }
            }
            return null;
        }
        /// <summary>
        ///  
        /// </summary>
        /// <returns></returns>
        public string GetScriptContent()
        {
            var fBuilder = new StringBuilder();
            var globalPipeline = new AggregateJobTree(_script);
            var aggGroups = new Dictionary<string, AggregateStage>();
            var sbGroups = new StringBuilder();
            var sbProjections = new StringBuilder();
            var groupHashes = new Dictionary<int, string>();
            //var allStages = _aggregateJobTrees.SelectMany(x => x.Stages);
            foreach (var aggJobTree in _aggregateJobTrees)
            {
                foreach (var stage in aggJobTree.Stages)
                {
                    var groups = stage.GetGroupings().ToList();
                    var projections = stage.GetProjections();
                    int iGrp = 0;
                    var stageJsonX = stage.WrapValue();
                    var stageJson = stage.WrapValueWithRoot(aggJobTree.Name);
                    foreach (var grp in groups)
                    {
                        var grpHash = Math.Abs(grp.GetHashCode());
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
                    switch (stage.Type)
                    {
                        case AggregateStageType.Group:
                            //TODO: provide group name
                            var stageWrap = stage.WrapValueWithRoot(aggJobTree.Name);
                            var gBsonDoc = $"groupFields.Merge(BsonDocument.Parse({stageWrap}));";
                            //Append our group to the projections so that it's visible
                            var pBsonDocFake = $"projections.Merge(new BsonDocument" + "{{\"" + aggJobTree.Name + "\", \"$" + aggJobTree.Name + "\"}}" + ");";
                            sbProjections.AppendLine(pBsonDocFake);
                            sbGroups.AppendLine(gBsonDoc);
                            break;
                        case AggregateStageType.Project:
                            var pBsonDoc = $"projections.Merge(BsonDocument.Parse({stageJson}));";
                            sbProjections.AppendLine(pBsonDoc);
                            break;
                        default:
                            throw new NotImplementedException("Support for root pipeline expression not implemented: " + stage.Type.ToString());
                    }
                }
            }
            fBuilder.AppendLine(sbGroups.ToString());
            fBuilder.AppendLine(sbProjections.ToString());
            var aggregatePipeline = GeneratePipeline();
            fBuilder.Append(aggregatePipeline);
            return fBuilder.ToString();
        }
        public string GeneratePipeline()
        {
            var fBuilder = new StringBuilder();
            if (!HasGroupingKeys && HasGroupingFields)
            {
                var groupKey = "";
                if (_rootIntegration != null && !string.IsNullOrEmpty(_rootIntegration.DataTimestampColumn))
                {
                    groupKey += "var idSubKey1 = new BsonDocument { { \"idKey\", \"$_id\" } };\n";
                    groupKey += "var idSubKey2 = new BsonDocument { { \"tsKey\", new BsonDocument{" +
                                "{ \"$dayOfYear\", \"$" + _rootIntegration.DataTimestampColumn + "\"}" +
                                "} } };\n";
                    groupKey += $"groupKeys.Merge(idSubKey1);\n" +
                                $"groupKeys.Merge(idSubKey2);\n" +
                                $"var grouping = new BsonDocument();\n" +
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
                record += $"var aggregateResult = rec{_rootDataMember.GetPropertyName()}.Aggregate<BsonDocument>(pipeline, aggOptions);";
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