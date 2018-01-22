﻿using System;
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
    public abstract class CodeGenerator
    {
        private static Assembly _assembly;

        public abstract string GenerateFromExpression(Expression mapReduce);
//        {
//            var expType = mapReduce.GetType();
//            if (expType == typeof(MapReduceExpression))
//            {
//                return GenerateReduceMap(mapReduce as MapReduceExpression);
//            }
//            else if (expType == typeof(MapAggregateExpression))
//            {
//                return GenerateReduceAggregate(mapReduce as MapAggregateExpression);
//            }
//            return null;
//        } 

        /// <summary>   Gets the contents of a template. </summary>
        ///
        /// <remarks>   Vasko, 14-Dec-17. </remarks>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ///
        /// <param name="name"> The name of the template file. </param>
        ///
        /// <returns>   A stream for the template. </returns>

        protected static Stream GetTemplate(string name)
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



        /// <summary>
        /// Generates variables from the given assignent expressions.
        /// </summary>
        /// <param name="expressions"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static IEnumerable<string> VisitVariables(IEnumerable<AssignmentExpression> expressions, StringBuilder value, ExpressionVisitor visitor)
        {
            var variables = new HashSet<string>();
            var tmpValue = String.Join(Environment.NewLine, expressions.Select(x =>
            {
                 var visitResult = visitor.CollectVariables(x);
                variables.AddRange(visitResult.Variables.Select(y => y.Key.Name));
                return visitResult.Value;
            }).ToArray());
            value.Append(tmpValue);
            return variables;
        }
    }
}