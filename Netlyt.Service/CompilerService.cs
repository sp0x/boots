using System;
using System.Reflection;
using Donut;
using Donut.Build;
using Donut.Lex.Data;
using Donut.Models;

namespace Netlyt.Service
{
    public class CompilerService
    {
        private Model _model;

        //private AssemblyLoadContext _context;
        public CompilerService()
        {
            //_context = null;
            //_context = AssemblyLoadContext.Default; //new DonutAssemblyLoadContext(asmDir); 
        }

        public Assembly Compile(DonutScript script, string assemblyName, out Type donutType, out Type donutContext, out Type featureGenerator)
        {
            var compiler = new DonutCompiler(script);
            string outputFile;
            var emitResult = compiler.Compile(assemblyName, out outputFile); 
            if (!emitResult.Result.Success)
            {
                throw new CompilationFailed(compiler.GetError(emitResult.Result));
            }
            Assembly asm = emitResult.GetAssembly(); 
            var scriptName = script.Type.GetClassName();
            var scriptContextName = script.Type.GetContextName();
            if (asm != null)
            {
                donutType = asm.GetType($"{assemblyName}.{scriptName}");
                donutContext = asm.GetType($"{assemblyName}.{scriptContextName}");
                featureGenerator = asm.GetType($"{assemblyName}.FeatureGenerator");
                script.AssemblyPath = asm.Location;
            }
            else
            {
                donutType = null;
                donutContext = null;
                featureGenerator = null;
            }
            return asm;
        }

        public void SetModel(Model model)
        {
            _model = model;
        }
    }
}
