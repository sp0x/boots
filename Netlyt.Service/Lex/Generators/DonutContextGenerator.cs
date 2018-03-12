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

        private void GetContextContent(Expression contextExpressionInfo, ref StringBuilder valueBuff)
        {
            if(valueBuff == null) valueBuff = new StringBuilder();
            //var lstValues = VisitVariables(contextExpressionInfo.Values, valueBuff, new JsGeneratingExpressionVisitor());
            //var valuesPart = String.Join(',', lstValues.Select(x => $"'{x}' : {x}\n").ToArray()) + '\n';
            //valueBuff.AppendLine("\nvar __value = { " + valuesPart + "};");
        }

        public string GenerateContext(string @namespace, DonutScript dscript)
        {
            string ctxTemplate;
            var baseName = dscript.Type.Name;
            using (StreamReader reader = new StreamReader(GetTemplate("DonutContext.txt")))
            {
                ctxTemplate = reader.ReadToEnd();
                if (string.IsNullOrEmpty(ctxTemplate)) throw new Exception("Template empty!");
                ctxTemplate = ctxTemplate.Replace("$Namespace", @namespace);
                ctxTemplate = ctxTemplate.Replace("$ClassName", baseName);
                var cacheSetMembers = GetCacheSetMembers(dscript);
                ctxTemplate = ctxTemplate.Replace("$CacheMembers", cacheSetMembers);
                var dataSetMembers = GetDataSetmembers(dscript);
                ctxTemplate = ctxTemplate.Replace("$DataSetMembers", dataSetMembers);
                var mappers = GetContextTypeMappers(dscript);
                ctxTemplate = ctxTemplate.Replace("$Mappers", mappers);

                //Items: $Namespace, $ClassName, $CacheMembers, $DataSetMembers, $Mappers 
            }
            return ctxTemplate;
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