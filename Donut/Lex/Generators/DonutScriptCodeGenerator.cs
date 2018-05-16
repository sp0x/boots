using System;
using System.IO;
using System.Linq;
using System.Text;
using Donut.Features;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Donut.Lex.Generation;
using MongoDB.Bson;
using Netlyt.Interfaces;

namespace Donut.Lex.Generators
{
    public class DonutScriptCodeGenerator : CodeGenerator
    {
        private AggregateFeatureGeneratingExpressionVisitor _expVisitor;
        //private DataIntegration _integration;

        public DonutScriptCodeGenerator(DonutScript script)
        {
            _expVisitor = new AggregateFeatureGeneratingExpressionVisitor(script);
            //_integration = integration;
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
            var encoding = System.Text.Encoding.UTF8;
            using (StreamReader reader = new StreamReader(GetTemplate("Donutfile.txt"), encoding))
            {
                donutTemplate = reader.ReadToEnd();
                if (string.IsNullOrEmpty(donutTemplate)) throw new Exception("Template empty!");
                var donutComment = $"//Generated on {DateTime.UtcNow} UTC\n";
                donutComment += $"//Root collection on {script.GetRootIntegration().Collection}\n";
                donutTemplate = $"{donutComment}\n" + donutTemplate;
                donutTemplate = donutTemplate.Replace("$Namespace", @namespace);
                donutTemplate = donutTemplate.Replace("$ClassName", baseName);
                donutTemplate = donutTemplate.Replace("$ContextTypeName", conutextName);
                var prepareScript = GeneratePrepareExtractionContent(script);
                donutTemplate = donutTemplate.Replace("$PrepareExtraction", prepareScript);
                donutTemplate = ProcessFeaturePrepContent(donutTemplate, script); //donutTemplate.Replace("$ExtractionBody", GetFeaturePrepContent(donutTemplate, script));
                donutTemplate = donutTemplate.Replace("$CompleteExtraction", GetFeatureExtractionCompletion(script));
                var propertiesContent = GenerateDonutPropertiesContent(!string.IsNullOrEmpty(prepareScript));
                donutTemplate = donutTemplate.Replace("$DonutProperties", propertiesContent);
                donutTemplate = donutTemplate.Replace("$OnFinished", GenerateOnDounutFinishedContent(script));
                donutTemplate += "\n\n" +
                                 "/* Donut script: \n" + script.ToString() +
                                 "\n*/";
                //Items: $ClassName, $ContextTypeName, $ExtractionBody, $OnFinished
            }
            return donutTemplate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hasPrepareStage"></param>
        /// <returns></returns>
        private string GenerateDonutPropertiesContent(bool hasPrepareStage)
        {
            var buff = new StringBuilder();
            var donutType = typeof(Donutfile<,>);
            if (hasPrepareStage)
            {
                buff.AppendLine("  HasPrepareStage = true;");
            }
            return buff.ToString();
        }


        private string GenerateOnDounutFinishedContent(DonutScript script)
        {
            return String.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        private string GeneratePrepareExtractionContent(DonutScript script)
        {
            var fBuilder = new StringBuilder();
            var rootIntegration = script.Integrations.FirstOrDefault();
            if (rootIntegration == null)
                throw new InvalidIntegrationException("Script has no integrations");
            if (rootIntegration.Fields == null || rootIntegration.Fields.Count == 0)
                throw new InvalidIntegrationException("Integration has no fields");
            //Get our record variables
            GetIntegrationRecordVars(script, fBuilder);
            var aggregates = new AggregateFeatureCodeGenerator(script, _expVisitor);
            var aggFeatures = script.Features.Where(x => x.IsDonutAggregateFeature());
            aggregates.AddAll(aggFeatures);
            var aggregatePipeline = aggregates.GetScriptContent();
            fBuilder.Append(aggregatePipeline);
            return fBuilder.ToString();
        }

        private static void GetIntegrationRecordVars(DonutScript script, StringBuilder fBuilder)
        {
            foreach (var integration in script.GetDatasetMembers())
            {
                var iName = integration.GetPropertyName();
                var record = $"var rec{iName} = this.Context.{iName}.Records;";
                fBuilder.AppendLine(record);
            }
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
            var donutFnResolver = new DonutFunctions();
            foreach (var feature in script.Features)
            {
                IExpression accessor = feature.Value;
                string fName = feature.Member.ToString();
                string featureContent = "";
                var featureFType = accessor.GetType();
                if (featureFType == typeof(NameExpression))
                {
                    var member = (accessor as NameExpression).Member?.ToString();
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
                if (!string.IsNullOrEmpty(featureContent)) fBuilder.AppendLine(featureContent);
            }
            return fBuilder.ToString();
        }

        public string GetAggregates(DonutFunctionType? typeFilter = null)
        {
            var sb = new StringBuilder();
            foreach (var aggregate in _expVisitor.Aggregates)
            {
                if (typeFilter != null && aggregate.Type != typeFilter.Value) continue;
                var str = aggregate.Content;
                sb.AppendLine(str.GetValue());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates the code that processes each incoming document, in order to gather information for features.
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        private string ProcessFeaturePrepContent(string donutfileContent, DonutScript script)
        {
            string aggregateKeyContent = GetAggregateKeyExtraction(script, "aggKeyBuff");
            var placeholder = "$ExtractionBody";
            var donutCV = new DonutFeatureGeneratingExpressionVisitor(script);
            var donutFeatureGen = new DonutFeatureCodeGenerator(script, donutCV);
            var donutFeatures = script.Features.Where(x => x.IsDonutNativeFeature());
            donutFeatureGen.AddAll(donutFeatures);
            var donutFeatureCode = donutFeatureGen.GetScriptContent(donutfileContent);
            donutFeatureCode.PrepareScript = aggregateKeyContent + donutFeatureCode.PrepareScript;
            donutfileContent = donutfileContent.Replace(placeholder, donutFeatureCode.PrepareScript);
            return donutfileContent;
        }

        /// <summary>
        /// Gets the content of aggregate keys
        /// </summary>
        /// <param name="script"></param>
        /// <param name="keyBuffName"></param>
        /// <returns></returns>
        private string GetAggregateKeyExtraction(DonutScript script, string keyBuffName)
        {
            var rootIgn = script.GetRootIntegration();
            var ignKeys = rootIgn.AggregateKeys;
            if (ignKeys == null || !ignKeys.Any())
            {
                throw new Exception("Integration has no aggregate keys!");
            }
            var keys = ignKeys.Where(x=>x.Operation!=null);
//            if (keys == null || !keys.Any())
//            {
//                throw new Exception("Integration has no evaluable aggregate keys!");
//            }
            var buffer = new StringBuilder();
            int iKey = 0;
            buffer.AppendLine($"var {keyBuffName} = new Dictionary<string, object>();");
            foreach (var key in keys)
            {
                var keyName = $"aggKey{iKey}";
                var evalFnName = $"{keyName}_fn";
                var evalFn = key.Operation.GetCallCode(evalFnName).ToString();
                //var keyVar = $"var {keyName} = {evalFnName}(document[\"{key.Arguments}\"]);";
                var keyLine = $"{keyBuffName}[\"{key.Name}\"] = {evalFnName}(document[\"{key.Arguments}\"]);";
                buffer.AppendLine(evalFn);
                buffer.AppendLine(keyLine);
                iKey++;
            }

            buffer.AppendLine($"var groupKey = Context.AddMetaGroup({keyBuffName});");
            return buffer.ToString();
        }

        private string GetFeatureExtractionCompletion(DonutScript script)
        {
            var code = "";
            return code;
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
            var dtSources = dscript.GetDatasetMembers();
            var content = new StringBuilder();
            foreach (var source in dtSources)
            {
                var sourceProperty = $"[SourceFromIntegration(\"{source.Name}\")]\n" +
                                     "public DataSet<BsonDocument> " + source.GetPropertyName() + " { get; set; }";
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