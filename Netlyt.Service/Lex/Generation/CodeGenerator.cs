using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Netlyt.Service.Lex.Expressions; 

//using Netlyt.Service.Lex.Templates;

namespace Netlyt.Service.Lex.Generation
{
    public class CodeGenerator
    {
        private static Assembly _assembly;
        public object GenerateFromExpression(MapReduceExpression mapReduce)
        {
            using (StreamReader reader = new StreamReader(GetTemplate("MapReduceTemplate.txt")))
            {
                string result = reader.ReadToEnd();
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

                result = result;
            }
            return null;
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
