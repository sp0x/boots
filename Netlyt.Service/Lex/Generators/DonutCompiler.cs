using System;
using System.Reflection;
using MongoDB.Bson;
using nvoid.db.Caching;
using Netlyt.Service.Build;
using Netlyt.Service.Donut;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Generation;

namespace Netlyt.Service.Lex.Generators
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

        public Assembly Compile(string assemblyName)
        {
            var generatedContext = _codeGen.GenerateContext(assemblyName, _script); 
            var builder = new CsCompiler(assemblyName);
            //Add our reference libs
            builder.AddReferenceFromType(typeof(BsonDocument));
            builder.AddReferenceFromType(typeof(DonutContext));
            builder.AddReferenceFromType(typeof(CacheSet<>));
            builder.AddReferenceFromType(typeof(IServiceProvider));

            var output = builder.CompileAndGetAssembly(generatedContext);
            return output;
        }
    }
}