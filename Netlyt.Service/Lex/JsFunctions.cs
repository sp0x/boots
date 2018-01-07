using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex
{
    public class JsFunctions
    {
        private static Dictionary<string, string> Functions { get; set; }
        static JsFunctions()
        {
            Functions = new Dictionary<string, string>();
            Functions["time"] = "(function(timeElem){ return timeElem.getTime() })";
        }
        public static string Resolve(string function, List<ParameterExpression> expParameters)
        {
            string output = null;
            if (Functions.ContainsKey(function))
            {
                output = Functions[function];
            }
            return output;
        }
    }
}
