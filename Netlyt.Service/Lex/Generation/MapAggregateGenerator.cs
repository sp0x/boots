using System;
using System.IO;
using System.Linq;
using System.Text;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex.Generation
{
    public class MapAggregateGenerator
        : CodeGenerator
    { 

        public override string GenerateFromExpression(Expression mapAggregate)
        {
            string reduceTemplate;
            using (StreamReader reader = new StreamReader(GetTemplate("MapReduceAggregate.txt")))
            {
                reduceTemplate = reader.ReadToEnd();
                if (reduceTemplate == null) throw new Exception("Template empty!"); 
                var valueBuff = new StringBuilder();
                var mapAggregateExpression = mapAggregate as MapAggregateExpression;
                GetMapAggregateContent(mapAggregateExpression, ref valueBuff);
                reduceTemplate = reduceTemplate.Replace("$value", valueBuff.ToString());
            }
            return reduceTemplate;
        }

        private static void GetMapAggregateContent(MapAggregateExpression mapReduce, ref StringBuilder valueBuff)
        { 
            if (valueBuff == null) valueBuff = new StringBuilder(); 
            var lstValues = VisitVariables(mapReduce.Values, valueBuff, new JsGeneratingExpressionVisitor()); 
            var valuesPart = String.Join(',', lstValues.Select(x => $"'{x}' : {x}\n").ToArray()) + '\n';
            valueBuff.AppendLine("\nvar __value = { " + valuesPart + "};");
        }

    }
}