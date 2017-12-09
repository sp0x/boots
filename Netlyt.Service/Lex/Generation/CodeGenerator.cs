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
        public object GenerateFromExpression(MapReduceExpression mapReduce)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Netlyt.Service.Lex.Templates.MapReduceTemplate.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
            }
            return null;
        }
    }

    public class DonutFileGenerator
    {
        private string _template;
        public DonutFileGenerator()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Netlyt.Service.Lex.Templates.Donutfile.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                _template = reader.ReadToEnd();
            }
        }
    }
}
