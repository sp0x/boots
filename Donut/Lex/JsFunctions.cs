using System;
using System.Collections.Generic;
using Donut.Lex.Expressions;
using Netlyt.Interfaces;

namespace Donut.Lex
{
    public class JsFunctions
    {
        private static Dictionary<string, string> Functions { get; set; }
        static JsFunctions()
        {
            Functions = new Dictionary<string, string>();
            Functions["time"] = "(function(timeElem){ return timeElem.getTime() })";
            Functions["selectMany"] = @"(function(array, memberGetter){
                                        var buff = [];
                                        array.forEach(function(a){
                                            var member = memberGetter(a); for(var i=0; i<member.length; i++) buff.push(member[i]);
                                        });
                                        return buff; })";
            Functions["if"] = "(function(condition, ifTrue, ifElse){ return condition ? (ifTrue) : (ifElse); })";
            Functions["any"] = "(function(array){ return array.length>0; })";
        }
        public static string Resolve(string function, List<ParameterExpression> expParameters)
        {
            string output = null;
            if (Functions.ContainsKey(function))
            {
                output = Functions[function];
            }
            else
            {
                throw new Exception($"Unsupported js function: {function}");
            }
            return output;
        }
    }
}
