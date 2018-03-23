using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Emit;
using MongoDB.Bson;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generation;

namespace Netlyt.Service.Lex.Generators
{
    public class DonutScriptCodeGenerator : CodeGenerator
    {
        private DonutFeatureGeneratingExpressionVisitor _expVisitor;
        private DataIntegration _integration;

        public DonutScriptCodeGenerator(DataIntegration integration)
        {
            _expVisitor = new DonutFeatureGeneratingExpressionVisitor(null);
            _integration = integration;
        }
        public override string GenerateFromExpression(Expression contextExpressionInfo)
        {
            return null;
        }

        /// <summary>
        /// Generates a donutfile context
        /// </summary>
        /// <param name="namespace"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public string GenerateContext(string @namespace, DonutScript script)
        {
            string ctxTemplate;
            _expVisitor.Clear();
            _expVisitor.SetScript(script);
            if (script.Type == null) throw new ArgumentException("Script type is null!");
            var baseName = script.Type.GetContextName();
            using (StreamReader reader = new StreamReader(GetTemplate("DonutContext.txt")))
            {
                ctxTemplate = reader.ReadToEnd();
                if (string.IsNullOrEmpty(ctxTemplate)) throw new Exception("Template empty!");
                ctxTemplate = ctxTemplate.Replace("$Namespace", @namespace);
                ctxTemplate = ctxTemplate.Replace("$ClassName", baseName);
                var cacheSetMembers = GetCacheSetMembers(script);
                ctxTemplate = ctxTemplate.Replace("$CacheMembers", cacheSetMembers);
                var dataSetMembers = GetDataSetMembers(script);
                ctxTemplate = ctxTemplate.Replace("$DataSetMembers", dataSetMembers);
                var mappers = GetContextTypeMappers(script);
                ctxTemplate = ctxTemplate.Replace("$Mappers", mappers);

                //Items: $Namespace, $ClassName, $CacheMembers, $DataSetMembers, $Mappers 
            }
            return ctxTemplate;
        }

        /// <summary>
        /// Generates a donutfile
        /// </summary>
        /// <param name="namespace"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public string GenerateDonut(string @namespace, DonutScript script)
        {
            string donutTemplate;
            var baseName = script.Type.GetClassName();
            var conutextName = script.Type.GetContextName();
            _expVisitor.Clear();
            _expVisitor.SetScript(script);
            using (StreamReader reader = new StreamReader(GetTemplate("Donutfile.txt")))
            {
                donutTemplate = reader.ReadToEnd();
                if (string.IsNullOrEmpty(donutTemplate)) throw new Exception("Template empty!");
                donutTemplate = donutTemplate.Replace("$Namespace", @namespace);
                donutTemplate = donutTemplate.Replace("$ClassName", baseName);
                donutTemplate = donutTemplate.Replace("$ContextTypeName", conutextName);
                donutTemplate = donutTemplate.Replace("$ExtractionBody", GetFeaturePrepContent(script));
                donutTemplate = donutTemplate.Replace("$OnFinished", GenerateOnDonutFinishedContent(script));
                //Items: $ClassName, $ContextTypeName, $ExtractionBody, $OnFinished
            }
            return donutTemplate;
        }

        private string GenerateOnDonutFinishedContent(DonutScript script)
        {
            var fBuilder = new StringBuilder();
            var donutFnResolver = new DonutFunctionParser();
            
            var rootIntegration = script.Integrations.FirstOrDefault();
            var rootCollection = rootIntegration.Name;
            var outputCollection = rootIntegration.FeaturesCollection;
            GetIntegrationRecordVars(script, fBuilder);
            bool hasGroupFields = false,
                hasProjection = false,
                hasGroupKeys = false;

            //Update this to work with multiple collections later on
            foreach (var feature in script.Features)
            {
                IExpression accessor = feature.Value;
                string fName = feature.Member.ToString();
                string featureContent = "";
                var featureFType = accessor.GetType();
                if (featureFType == typeof(VariableExpression))
                {
                    var member = (accessor as VariableExpression).Member?.ToString();
                    //In some cases we might just use the field
                    if (string.IsNullOrEmpty(member)) member = accessor.ToString();
                    featureContent = $"groupFields[\"{fName}\"] = " + "new BsonDocument { { \"$first\", \"${member}\" } };";
                }
                else if (featureFType == typeof(CallExpression))
                {
                    if (donutFnResolver.IsAggregate(accessor as CallExpression))
                    {
                        //We're dealing with an aggregate call 
                        var aggregateContent = GenerateFeatureFunctionCall(accessor as CallExpression);
                        var functionType = donutFnResolver.GetFunctionType(accessor as CallExpression);
                        var aggregateValue = aggregateContent?.GetValue().Replace("$"+rootCollection+".","$");
                        if (aggregateValue != null) aggregateValue = aggregateValue.Replace("\"", "\\\"");
                        switch (functionType)
                        {
                            case DonutFunctionType.Group:
                                featureContent = $"groupFields[\"{fName}\"] = BsonDocument.Parse(\"{aggregateValue}\");";
                                hasGroupFields = true;
                                break;
                            case DonutFunctionType.Project:
                                featureContent = $"projections[\"{fName}\"] = \"{aggregateValue}\";";
                                hasProjection = true;
                                break;
                            case DonutFunctionType.GroupKey:
                                if (!string.IsNullOrEmpty(aggregateValue))
                                { 
                                    featureContent = $"groupKeys[\"{fName}\"] = \"{aggregateValue}\";";
                                    hasGroupKeys = true;
                                }
                                break;
                            case DonutFunctionType.Standard:
                                var variableName = GetFeatureVariableName(feature);
                                featureContent = $"groupFields[\"{fName}\"] = new BsonDocument" + "{{ \"$first\", \"$" + variableName  + "\" }};";
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
                if(!string.IsNullOrEmpty(featureContent)) fBuilder.AppendLine(featureContent);
            }
            if (!hasGroupKeys && hasGroupFields)
            {
                var groupKey = "";
                if (rootIntegration != null && !string.IsNullOrEmpty(rootIntegration.DataTimestampColumn))
                {
                    groupKey += "var idSubKey1 = new BsonDocument { { \"idKey\", \"$_id\" } };\n";
                    groupKey += "var idSubKey2 = new BsonDocument { { \"tsKey\", new BsonDocument{" +
                                "{ \"$dayOfYear\", \"$" + rootIntegration.DataTimestampColumn + "\"}" +
                                "} } };\n"; 
                    groupKey += $"groupKeys.Merge(idSubKey1);\n" +
                                $"groupKeys.Merge(idSubKey2);\n" +
                                $"var grouping = new BsonDocument();\n" +
                                $"grouping[\"_id\"] = groupKeys;\n" +
                                $"grouping = grouping.Merge(groupingFields);";
                }
                else
                {
                    groupKey = $"groupKeys[\"_id\"] = \"$_id\";\n";
                }
                fBuilder.AppendLine(groupKey);
            }

            if (hasGroupFields || hasProjection)
            {
                if (hasGroupFields)
                {
                    var groupStep = @"pipeline.Add(new BsonDocument{
                                        {" + "\"$group\", grouping}"+
                                        "});";
                    fBuilder.AppendLine(groupStep);
                }
                if (hasProjection)
                {
                    var projectStep = @"pipeline.Add(new BsonDocument{
                                        {" + "\"$project\", projections} " +
                                        "});";
                    fBuilder.AppendLine(projectStep);
                }
                var outputStep = @"pipeline.Add(new BsonDocument{
                                {" + "\"$out\", \"" + outputCollection + "\"" + "}" +
                                "});";
                fBuilder.AppendLine(outputStep);
                var record = $"var aggregateResult = rec{rootCollection}.Aggregate<BsonDocument>(pipeline);";
                fBuilder.AppendLine(record);
            }

            return fBuilder.ToString();
        }

        private static void GetIntegrationRecordVars(DonutScript script, StringBuilder fBuilder)
        {
            foreach (var integration in script.Integrations)
            {
                var iName = integration.Name.Replace(" ", "_");
                var record = $"var rec{iName} = this.Context.{iName}.Records;";
                fBuilder.AppendLine(record);
            }
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
            while (itemQueue.Count>0)
            {
                var item = itemQueue.Dequeue();
                var memberInfo = item.GetType();
                if (memberInfo == typeof(CallExpression))
                {
                    var subItems = (item as CallExpression).Parameters;
                    foreach (var param in subItems) itemQueue.Enqueue(param);
                } else if (memberInfo == typeof(VariableExpression))
                {
                    var member = (item as VariableExpression).Member?.ToString();
                    string memberName = !string.IsNullOrEmpty(member) ? member : (item as VariableExpression).Name;
                    return memberName;
                } else if (memberInfo == typeof(ParameterExpression))
                {
                    itemQueue.Enqueue((item as ParameterExpression).Value);
                }
            }
            return null;
        }

        public string GenerateFeatureGenerator(string @namespace, DonutScript script)
        {
            string fgenTemplate;
            var donutName = script.Type.GetClassName();
            var conutextName = script.Type.GetContextName();
            _expVisitor.Clear();
            _expVisitor.SetScript(script);
            using (StreamReader reader = new StreamReader(GetTemplate("FeatureGenerator.txt")))
            {
                fgenTemplate = reader.ReadToEnd();
                if (string.IsNullOrEmpty(fgenTemplate)) throw new Exception("Template empty!");
                fgenTemplate = fgenTemplate.Replace("$Namespace", @namespace);
                fgenTemplate = fgenTemplate.Replace("$DonutType", donutName);
                fgenTemplate = fgenTemplate.Replace("$DonutContextType", conutextName);
                fgenTemplate = fgenTemplate.Replace("$ContextTypeName", conutextName);
                fgenTemplate = fgenTemplate.Replace("$FeatureYields", GetFeatureYieldsContent(script));
                //Items: $Namespace, $DonutType, $FeatureYields
            }
            return fgenTemplate;
        }
        private string GetFeatureYieldsContent(DonutScript script)
        {
            var fBuilder = new StringBuilder();
            var donutFnResolver = new DonutFunctionParser();
            foreach (var feature in script.Features)
            {
                IExpression accessor = feature.Value;
                string fName = feature.Member.ToString();
                string featureContent = "";
                var featureFType = accessor.GetType();
                if (featureFType == typeof(VariableExpression))
                {
                    var member = (accessor as VariableExpression).Member?.ToString();
                    //In some cases we might just use the field
                    if (string.IsNullOrEmpty(member)) member = accessor.ToString();
                    featureContent = $"yield return pair(\"{fName}\", doc[\"{member}\"]);";
                }
                else if (featureFType == typeof(CallExpression))
                {
                    if (donutFnResolver.IsAggregate(accessor as CallExpression))
                    {
                        //We're dealing with an aggregate call 
                        //var aggregateContent = GenerateFeatureFunctionCall(accessor as CallExpression, feature);

                    }
                    else
                    {
                        //We're dealing with a function call 
                        //featureContent = GenerateFeatureFunctionCall(accessor as CallExpression, feature).Content;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                if(!string.IsNullOrEmpty(featureContent)) fBuilder.AppendLine(featureContent);
            }
            return fBuilder.ToString();
        }

        public GeneratedFeatureFunctionsCodeResult GenerateFeatureFunctionCall(CallExpression callExpression, AssignmentExpression feature=null)
        {
            var cshGenerator = new DonutCSharpGenerator();
            var donutFnResolver = new DonutFunctionParser();
            var isAggregate = donutFnResolver.IsAggregate(callExpression);
            var functionType = donutFnResolver.GetFunctionType(callExpression);
            var output = cshGenerator.ProcessCall(callExpression, _expVisitor);
            if (string.IsNullOrEmpty(output))
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
                        aggregateField = BsonDocument.Parse(output);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                        return null;
                    }
                }
                else
                {
                    aggregateField[feature.Member.ToString()] = BsonDocument.Parse(output);
                }
                output = aggregateField.ToString();
                var result = new GeneratedFeatureFunctionsCodeResult(null);
                if (functionType == DonutFunctionType.Group)
                {
                    result.GroupFields = output;
                }
                else if(functionType==DonutFunctionType.Project)
                {
                    result.Projections = output;
                }
                else if (functionType == DonutFunctionType.GroupKey)
                {
                    result.GroupKeys = output;
                }
                return result;
            }
            else
            {
                return new GeneratedFeatureFunctionsCodeResult(output);
            }
        }

        public string GetAggregates(DonutFunctionType? typeFilter = null)
        {
            var sb = new StringBuilder();
            foreach (var aggregate in _expVisitor.Aggregates)
            {
                if (typeFilter != null && aggregate.Type != typeFilter.Value) continue;
                var str = aggregate.Content;
                sb.AppendLine(str);
            }
            return sb.ToString();
        }

        private string GetFeaturePrepContent(DonutScript script)
        {
            return "";
        }

        private string GetContextTypeMappers(DonutScript dscript)
        {
            //Template: 
            //RedisCacher.RegisterCacheMap<MapTypeName, TypeToMapNamme>
            var sb = new StringBuilder();
            return sb.ToString();
        }

        private string GetDataSetMembers(DonutScript dscript)
        {
            var secondarySources = dscript.Integrations;//.Skip(1);
            var content = new StringBuilder();
            foreach (var source in secondarySources)
            {
                var sName = source.Name.Replace(' ', '_');
                var sourceProperty = $"[SourceFromIntegration(\"{source.Name}\")]\n" +
                                     "public DataSet<BsonDocument> " + sName + " { get; set; }";
                content.AppendLine(sourceProperty);
            }
            return content.ToString();
        }

        private string GetCacheSetMembers(DonutScript dscript)
        {
            var featureAssignments = dscript.Features;
            var content = new StringBuilder();
            foreach (var fassign in featureAssignments)
            {
                var name = fassign.Member.Name;
                var sName = name.Replace(' ', '_');
                var typeName = "string";
                //Resolve the type name if needed
                var sourceProperty = $"public CacheSet<{typeName}> " + sName + " { get; set; }";
                content.AppendLine(sourceProperty);
            }
            return content.ToString();
        }

    }
}