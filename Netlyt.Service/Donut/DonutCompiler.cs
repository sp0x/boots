using System;
using System.Reflection;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using nvoid.db.Caching;
using Netlyt.Service.Build;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Generation;
using Netlyt.Service.Lex.Generators;

namespace Netlyt.Service.Donut
{
    public class DonutCompiler
    { 
        private DonutScript _script;
        private DonutScriptCodeGenerator _codeGen; 

        public DonutCompiler(DonutScript dscript)
        {
            _script = dscript; 
            _codeGen = dscript.GetCodeGenerator() as DonutScriptCodeGenerator;   
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyName">The compiled assembly</param>
        /// <param name="donutType">The donut type</param>
        /// <param name="donutContext">The donut's context type</param>
        /// <param name="featureGenerator">The feature generator type</param>
        /// <returns></returns>
        public Assembly Compile(string assemblyName, out Type donutType, out Type donutContext, out Type featureGenerator)
        {
            var generatedContext = _codeGen.GenerateContext(assemblyName, _script);
            var generatedDonut = _codeGen.GenerateDonut(assemblyName, _script);
            var generatedFeatureGen = _codeGen.GenerateFeatureGenerator(assemblyName, _script);
            var builder = new CsCompiler(assemblyName);
            //Add our reference libs
            builder.AddReferenceFromType(typeof(BsonDocument));
            builder.AddReferenceFromType(typeof(DonutContext));
            builder.AddReferenceFromType(typeof(CacheSet<>));
            builder.AddReferenceFromType(typeof(IServiceProvider));
            builder.AddReferenceFromType(typeof(nvoid.extensions.Arrays));
            builder.AddReferenceFromType(typeof(System.Linq.Enumerable));
            builder.AddReferenceFromType(typeof(TransformBlock<,>));
            var clrDep =
                Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
            builder.AddReference(clrDep);

            var output = builder.CompileAndGetAssembly(generatedContext, generatedDonut, generatedFeatureGen);
            var scriptName = _script.Type.GetClassName();
            var scriptContextName = _script.Type.GetContextName();
            if (output != null)
            {
                donutType = output.GetType($"{assemblyName}.{scriptName}");
                donutContext = output.GetType($"{assemblyName}.{scriptContextName}");
                featureGenerator = output.GetType($"{assemblyName}.FeatureGenerator");
            }
            else
            {
                donutType = null;
                donutContext = null;
                featureGenerator = null;
            }
            return output;
        }
    }
}