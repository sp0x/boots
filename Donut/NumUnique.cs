using System;
using System.Linq;
using System.Text;
using Donut.Crypto;
using Donut.Features;
using Donut.Lex.Expressions;

namespace Donut
{
    /// <summary>
    /// Nuber of unique values of an expression
    /// </summary>
    public class NumUnique : DonutFunction, IDonutTemplateFunction<DonutCodeFeatureDefinition>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nm"></param>
        public NumUnique(string nm) : base(nm)
        {
        }

        public DonutCodeFeatureDefinition GetTemplate(CallExpression exp, DonutCodeContext ctx)
        {
            var rootIgn = ctx.Script.GetRootIntegration();
            var expVisitor = new DonutFeatureGeneratingExpressionVisitor(ctx.Script);
            var callParam = exp.Parameters.FirstOrDefault();
            var parameterTarget = expVisitor.Visit(callParam);
            var buffGather = new StringBuilder();
            var buffExtract = new StringBuilder();
            var paramHash = HashAlgos.Adler32(callParam.ToString());
            string featureKey = $"nu_{rootIgn.Name}_{paramHash}"; 
            var categoryValue = 0;
            string ordered = "true";
            string varCategory = $"{featureKey}_cat";
            string varValue = $"{featureKey}_val";
            string varCategoryVal = $"var {varCategory} = {categoryValue};";
            string varValueVal = $"var {varValue} = {parameterTarget}.ToString();";

            buffGather.AppendLine(varCategoryVal);
            buffGather.AppendLine(varValueVal);
            buffGather.AppendLine($"Context.AddEntityMetaCategory(\"{featureKey}\", {varCategory}, {varValue}, {ordered});");

            buffExtract.AppendLine($"Context.GetSetSize(\"{featureKey}\")");
            var featureDef = new DonutCodeFeatureDefinition()
            {
                PrepareScript = buffGather.ToString(),
                ExtractionScript = buffExtract.ToString()
            };
            //Feature value:
            //Context.GetSetSize()
            //
            //Define a nameOfTheResultCache, category, value
            //Set the value in the cache
            return featureDef;
            /**
             * Example 
               //Extraction goes in here
               var nuRomanianpm25Category = 1;
               var nuRomanianpm25Value = intDoc["pm25"].ToString();
               Context.AddEntityMetaCategory("nu_Romanian.pm25", nuRomanianpm25Category, nuRomanianpm25Value, true);
             */
        }

        public override int GetHashCode()
        {
            var content = GetValue();
            return (int)HashAlgos.Adler32(content);
        }
    }
}