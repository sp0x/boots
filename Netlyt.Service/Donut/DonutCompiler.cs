using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using MongoDB.Bson;
using nvoid.db.Caching;
using Netlyt.Service.Build; 
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Generation;
using Netlyt.Service.Lex.Generators;
using System.Linq;
using MongoDB.Driver;
using Netlyt.Service.Ml;

namespace Netlyt.Service.Donut
{
    public class DonutCompiler
    { 
        private DonutScript _script;
        private DonutScriptCodeGenerator _codeGen;
        private string _assembliesDir;

        public DonutCompiler(DonutScript dscript)
        {
            _script = dscript; 
            _codeGen = dscript.GetCodeGenerator() as DonutScriptCodeGenerator;
            _assembliesDir = Path.Combine(Environment.CurrentDirectory, "donutAssemblies");
        }

        private void WriteDonutCode(string assemblyName, string fileName, string content)
        {
            var dir = Path.Combine(_assembliesDir, assemblyName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var filepath = Path.Combine(dir, fileName);
            File.WriteAllText(filepath, content);
        }

        public EmitResultAssembly Compile(string assemblyName, out string filePath)
        {
            var generatedContext = _codeGen.GenerateContext(assemblyName, _script);
            var generatedDonut = _codeGen.GenerateDonut(assemblyName, _script);
            var generatedFeatureGen = _codeGen.GenerateFeatureGenerator(assemblyName, _script);
#if DEBUG
            WriteDonutCode(assemblyName, "DonutContext.cs", generatedContext);
            WriteDonutCode(assemblyName, "DonutFile.cs", generatedDonut);
            WriteDonutCode(assemblyName, "FeatureEmitter.cs", generatedFeatureGen); 
#endif
            var builder = new CsCompiler(assemblyName, _assembliesDir);
            //Add our reference libs
            builder.AddReferenceFromType(typeof(BsonDocument));
            builder.AddReferenceFromType(typeof(DonutContext));
            builder.AddReferenceFromType(typeof(CacheSet<>));
            builder.AddReferenceFromType(typeof(Dictionary<,>));
            builder.AddReferenceFromType(typeof(IServiceProvider));
            builder.AddReferenceFromType(typeof(nvoid.extensions.Arrays));
            builder.AddReferenceFromType(typeof(System.Linq.Enumerable));
            builder.AddReferenceFromType(typeof(TransformBlock<,>));
            builder.AddReferenceFromType(typeof(IMongoCollection<>));
            var clrDep =
                Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51");
            var asmCollections = Assembly.Load("System.Collections");
            builder.AddReference(clrDep);
            builder.AddReference(asmCollections);
#if DEBUG
            var projectDefinition = builder.GenerateProjectDefinition(assemblyName, _script);
            WriteDonutCode(assemblyName, $"{assemblyName}.csproj", projectDefinition);
#endif
            var emitResult = builder.Compile(generatedContext, generatedDonut, generatedFeatureGen);
            filePath = builder.Filepath;
            return emitResult;
        }

        public string GetError(EmitResult er)
        {
            IEnumerable<Diagnostic> failures = er.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);
            var message = new StringBuilder();
            foreach (Diagnostic diagnostic in failures)
            {
                message.AppendFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
            }
            return message.ToString();
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
            string resultingFile;
            var emitResult = Compile(assemblyName, out resultingFile);
            Assembly asm = null;
            if (!emitResult.Result.Success)
            {
                throw new CompilationFailed(GetError(emitResult.Result));
            }
            else
            {
                asm = emitResult.GetAssembly(); 
            } 
            var scriptName = _script.Type.GetClassName();
            var scriptContextName = _script.Type.GetContextName();
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