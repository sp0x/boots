using System;
using System.IO;
using System.Linq;
using System.Text;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generation;

namespace Netlyt.Service.Lex.Generators
{

    /// <summary>
    /// Deals with generating the map code in a map-reduce job.
    /// </summary>
    public class MapReduceMapGenerator : CodeGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapReduce"></param>
        /// <param name="keyBuff"></param>
        /// <param name="valueBuff"></param>
        private static void GetMapReduceContent(MapReduceExpression mapReduce, ref StringBuilder keyBuff, ref StringBuilder valueBuff)
        {
            if (keyBuff == null) keyBuff = new StringBuilder();
            if (valueBuff == null) valueBuff = new StringBuilder();
            var lstKeys = VisitVariables(mapReduce.Keys, keyBuff, new JsGeneratingExpressionVisitor());
            var lstValues = VisitVariables(mapReduce.ValueMembers, valueBuff, new JsGeneratingExpressionVisitor());
            var keysPart = String.Join(',', lstKeys.Select(x => $"'{x}' : {x}").ToArray()) + '\n';
            var valuesPart = String.Join(',', lstValues.Select(x => $"'{x}' : {x}").ToArray()) + '\n';
            keyBuff.AppendLine("var __key = {  " + keysPart + "};");
            valueBuff.AppendLine("var __value = { " + valuesPart + "};");
        } 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapReduce"></param>
        /// <returns></returns>
        public override string GenerateFromExpression(Expression mapReduce)
        {
            string reduceTemplate;
            using (StreamReader reader = new StreamReader(GetTemplate("MapReduceMapper.txt")))
            {
                reduceTemplate = reader.ReadToEnd();
                if (reduceTemplate == null) throw new Exception("Template empty!");
                var keyBuff = new StringBuilder();
                var valueBuff = new StringBuilder();
                var mapReduceExpression = mapReduce as MapReduceExpression;
                GetMapReduceContent(mapReduceExpression, ref keyBuff, ref valueBuff);
                reduceTemplate = reduceTemplate.Replace("$key", keyBuff.ToString());
                reduceTemplate = reduceTemplate.Replace("$value", valueBuff.ToString());
            }
            return reduceTemplate;
        }
    }
}