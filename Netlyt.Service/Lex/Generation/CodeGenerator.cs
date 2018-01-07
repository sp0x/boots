using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Netlyt.Service.Lex.Expressions; 
using nvoid.extensions;

//using Netlyt.Service.Lex.Templates;

namespace Netlyt.Service.Lex.Generation
{
    public class CodeGenerator
    {
        private static Assembly _assembly;

        public string GenerateReduceMap(MapReduceExpression mapReduce)
        {
            string reduceTemplate;
            using (StreamReader reader = new StreamReader(GetTemplate("MapReduceMapper.txt")))
            {
                reduceTemplate = reader.ReadToEnd();
                if (reduceTemplate == null) throw new Exception("Template empty!");
                var keyBuff = new StringBuilder();
                var valueBuff = new StringBuilder();
                GetMapReduceContent(mapReduce, ref keyBuff, ref valueBuff);
                reduceTemplate = reduceTemplate.Replace("$key", keyBuff.ToString());
                reduceTemplate = reduceTemplate.Replace("$value", valueBuff.ToString());
            }
            return reduceTemplate;
        }

        public string GenerateReduceAggregate(MapAggregateExpression x)
        {
            string reduceTemplate;

            using (StreamReader reader = new StreamReader(GetTemplate("MapReduceReducer.txt")))
            {
                reduceTemplate = reader.ReadToEnd();
                if (reduceTemplate == null) throw new Exception("Template empty!");
                var keyBuff = new StringBuilder();
                var valueBuff = new StringBuilder();
                GetMapReduceAggregateContent(x, ref valueBuff);
                reduceTemplate = reduceTemplate.Replace("$key", keyBuff.ToString());
                reduceTemplate = reduceTemplate.Replace("$value", valueBuff.ToString());
            }
            return reduceTemplate;
        }

        public string GenerateFromExpression(Expression mapReduce)
        {
            var expType = mapReduce.GetType();
            if (expType == typeof(MapReduceExpression))
            {
                return GenerateReduceMap(mapReduce as MapReduceExpression);
            }
            else if (expType == typeof(MapAggregateExpression))
            {
                return GenerateReduceAggregate(mapReduce as MapAggregateExpression);
            }
            //TODO: generate a map and a reduce js functions similar to these.
            // fill in the template with the functions
            var map = @"
function () {    
  var time = parseInt((this.ondate.getTime() / 1000) / (60 * 60 * 24));
  var eventData = [{ ondate : this.ondate, value : this.value, type : this.type }];
  emit({ uuid : this.uuid, day : time }, { 
    uuid : this.uuid,
    day : time,
    noticed_date : this.ondate,
    events : eventData
  });
}";
                var reduce = @"
function (key, values) {
  var elements = [];
  var startTime = null;
  values.forEach(function(a){ 
	for(var i=0; i<a.events.length;i++) elements.push(a.events[i]);    
  });  
  if(startTime==null && elements.length>0) startTime = elements[0].ondate;
  return {
uuid : key.uuid,
day : key.day,
noticed_date : startTime,
events : elements };
}";

                reduce = reduce;
            
            return null;
        }

        private static List<string> VisitVariables(IEnumerable<AssignmentExpression> expressions, StringBuilder value)
        {
            var variables = new List<string>();
            var tmpValue = String.Join(Environment.NewLine, expressions.Select(x =>
            {
                var visitResult = (new JsGeneratingExpressionVisitor()).CollectVariables(x);
                variables.AddRange(visitResult.Variables.Select(y=>y.Key.Name));
                return visitResult.Value;
            }).ToArray());
            value.Append(tmpValue);
            return variables;
        }

        private static void GetMapReduceContent(MapReduceExpression mapReduce, ref StringBuilder keyBuff, ref StringBuilder valueBuff)
        { 
            if (keyBuff == null) keyBuff = new StringBuilder();
            if (valueBuff == null) valueBuff = new StringBuilder();
            var lstKeys = VisitVariables(mapReduce.Keys, keyBuff);
            var lstValues = VisitVariables(mapReduce.ValueMembers, valueBuff);
            var keysPart = String.Join(',', lstKeys.Select(x => $"'{x}' : {x}").ToArray()) + '\n';
            var valuesPart = String.Join(',', lstValues.Select(x => $"'{x}' : {x}").ToArray()) + '\n';
            keyBuff.AppendLine("var __key = {  " + keysPart + "};");
            valueBuff.AppendLine("var __value = { " + valuesPart + "};");
        }

        private static void GetMapReduceAggregateContent(MapAggregateExpression mapAggregate, ref StringBuilder valueBuff)
        {
            if (valueBuff == null) valueBuff = new StringBuilder();
            var lstValues = VisitVariables(mapAggregate.Values, valueBuff);
            lstValues = lstValues;
        }

        /// <summary>   Gets the contents of a template. </summary>
        ///
        /// <remarks>   Vasko, 14-Dec-17. </remarks>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ///
        /// <param name="name"> The name of the template file. </param>
        ///
        /// <returns>   A stream for the template. </returns>

        private static Stream GetTemplate(string name)
        {
            if(_assembly==null) _assembly = Assembly.GetExecutingAssembly(); 
            var resourceName = $"Netlyt.Service.Lex.Templates.{name}"; 
            Stream stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new Exception("Template not found!");
            }
            //StreamReader reader = new StreamReader(stream);
            return stream;
        }
    }
}
