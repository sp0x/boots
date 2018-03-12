using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Emit;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generation;

namespace Netlyt.Service.Lex.Generators
{
    public class DonutScriptCodeGenerator : CodeGenerator
    {
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
            using (StreamReader reader = new StreamReader(GetTemplate("FeatureGenerator.txt")))
            {
                fgenTemplate = reader.ReadToEnd();
                if (string.IsNullOrEmpty(fgenTemplate)) throw new Exception("Template empty!");
                fgenTemplate = fgenTemplate.Replace("$Namespace", @namespace);
                fgenTemplate = fgenTemplate.Replace("$DonutType", donutName);
                fgenTemplate = fgenTemplate.Replace("$ContextTypeName", conutextName);
                fgenTemplate = fgenTemplate.Replace("$FeatureYields", GetFeatureYieldsContent(script));
                //Items: $Namespace, $DonutType, $FeatureYields
            }
            return fgenTemplate;
        }
        private string GetFeatureYieldsContent(DonutScript script)
        {
            return "";
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
                                     "public DataSet<BsonDocument> " + sName  + " { get; set; }";
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