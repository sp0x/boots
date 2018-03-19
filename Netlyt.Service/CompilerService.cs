using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Netlyt.Service.Build;
using Netlyt.Service.Donut;
using Netlyt.Service.Lex.Data;

namespace Netlyt.Service
{
    public class CompilerService
    {
        //private AssemblyLoadContext _context;
        public CompilerService()
        {
            var asmDir = Environment.CurrentDirectory;
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
            }
            else
            {
                donutType = null;
                donutContext = null;
                featureGenerator = null;
            }
            return asm;
        }
    }
}
