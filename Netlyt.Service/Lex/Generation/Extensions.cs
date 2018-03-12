using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Generators;

namespace Netlyt.Service.Lex.Generation
{
    public static class Extensions
    {
        private static Dictionary<Type, Func<CodeGenerator>> _generators;
        static Extensions()
        {
            _generators = new Dictionary<Type, Func<CodeGenerator>>();
            _generators.Add(typeof(MapReduceExpression), () => new MapReduceMapGenerator());
            _generators.Add(typeof(MapAggregateExpression), () => new MapReduceAggregateGenerator());
            _generators.Add(typeof(DonutScript), () => new DonutScriptCodeGenerator());
        }

        /// <summary>
        /// Gets the appropriate code generator that can generate code form the expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static CodeGenerator GetCodeGenerator(this IExpression expression)
        {
            var expType = expression.GetType();
            Func<CodeGenerator> generatorFunc;
            if (_generators.TryGetValue(expType, out generatorFunc))
            {
                var generator = generatorFunc();
                return generator;
            } 
            else
            {
                throw new Exception("No generator for this expression!");
            }
        }
    }
}
