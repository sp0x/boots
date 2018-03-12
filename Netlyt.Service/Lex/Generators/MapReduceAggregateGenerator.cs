using System;
using System.IO;
using System.Linq;
using System.Text;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generation;

namespace Netlyt.Service.Lex.Generators
{
    /// <summary>
    /// Deals with generating the reduce code in a map-reduce job.
    /// </summary>
    public class MapReduceAggregateGenerator : CodeGenerator
    { 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapAggregate"></param>
        /// <returns></returns>
        public override string GenerateFromExpression(Expression mapAggregate)
        {
            string reduceTemplate;
            using (StreamReader reader = new StreamReader(GetTemplate("MapReduceAggregate.txt")))
            {
                reduceTemplate = reader.ReadToEnd();
                if (string.IsNullOrEmpty(reduceTemplate)) throw new Exception("Template empty!"); 
                var valueBuff = new StringBuilder();
                var mapAggregateExpression = mapAggregate as MapAggregateExpression;
                GetReduceAggregateContent(mapAggregateExpression, ref valueBuff);
                reduceTemplate = reduceTemplate.Replace("$value", valueBuff.ToString());
            }
            return reduceTemplate;
        }

        private static void GetReduceAggregateContent(MapAggregateExpression mapReduce, ref StringBuilder valueBuff)
        { 
            if (valueBuff == null) valueBuff = new StringBuilder(); 
            var lstValues = VisitVariables(mapReduce.Values, valueBuff, new JsGeneratingExpressionVisitor()); 
            var valuesPart = String.Join(',', lstValues.Select(x => $"'{x}' : {x}\n").ToArray()) + '\n';
            valueBuff.AppendLine("\nvar __value = { " + valuesPart + "};");
        }

    }
}