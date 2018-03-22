using System;
using System.Collections.Generic;
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
                var dataSetMembers = GetDataSetmembers(script);
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
                //Items: $ClassName, $ContextTypeName, $ExtractionBody
            }
            return donutTemplate;
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
                        var aggregateContent = GenerateFeatureFunctionCall(accessor as CallExpression, feature);

                    }
                    else
                    {
                        //We're dealing with a function call 
                        featureContent = GenerateFeatureFunctionCall(accessor as CallExpression, feature).Content;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                fBuilder.AppendLine(featureContent);
            }
            return fBuilder.ToString();
        }

        public GeneratedFeatureFunctionsCodeResult GenerateFeatureFunctionCall(CallExpression callExpression, AssignmentExpression feature)
        {
            var cshGenerator = new DonutCSharpGenerator();
            var donutFnResolver = new DonutFunctionParser();
            var isAggregate = donutFnResolver.IsAggregate(callExpression);
            var functionType = donutFnResolver.GetFunctionType(callExpression);
            var output = cshGenerator.ProcessCall(callExpression, _expVisitor);
            if (isAggregate)
            {
                var aggregateField = new BsonDocument();
                aggregateField[feature.Member.ToString()] = BsonDocument.Parse(output);
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

        private string GetDataSetmembers(DonutScript dscript)
        {
            var secondarySources = dscript.Integrations.Skip(1);
            var content = new StringBuilder();
            foreach (var source in secondarySources)
            {
                var sName = source.Replace(' ', '_');
                var sourceProperty = $"[SourceFromIntegration(\"{source}\")]\n" +
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