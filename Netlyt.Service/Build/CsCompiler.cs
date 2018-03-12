using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Netlyt.Service.Build
{
    /// <summary>   A c# assembly builder. </summary>
    ///
    /// <remarks>   Vasko, 09-Dec-17. </remarks>

    public class CsCompiler
    {
        private List<MetadataReference> References { get; set; } 
        private string _assemblyName;

        /// <summary>   The filepath of the output assembly. </summary> 
        public string Filepath
        {
            get
            {
                var filename = $"{AssemblyName}.dll";
                var path = System.IO.Path.GetFullPath(filename);
                return path;
            }
        }

        /// <summary>   Gets or sets the name of the assembly. </summary>
        ///
        /// <value> The name of the assembly. </value>

        public string AssemblyName
        {
            get { return _assemblyName; }
            set
            {
                //TODO: Filter
                _assemblyName = value;
            }
        }

        public CsCompiler AddReference(string filePath)
        {
            var peReference = MetadataReference.CreateFromFile(filePath);
            References.Add(peReference);
            return this;
        }

        public CsCompiler AddReference(Assembly assembly)
        {
            var peReference = MetadataReference.CreateFromFile(assembly.Location);
            References.Add(peReference);
            return this;
        }
        public CsCompiler AddReferenceFromType(Type type)
        {
            AddReference(type.Assembly.Location);
            return this;
        }
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   vasko, 09-Dec-17. </remarks>
        ///
        /// <param name="assemblyName"> Name of the assembly to generate. </param>

        public CsCompiler(string assemblyName)
        {
            References = new List<MetadataReference>();
            AddReference(Assembly.Load("System.Runtime").Location);
            AddReference(typeof(object).GetTypeInfo().Assembly.Location);
            AddReference(typeof(Hashtable).GetTypeInfo().Assembly.Location);
            AddReference(typeof(Console).GetTypeInfo().Assembly.Location); 
            _assemblyName = assemblyName;
        }

        /// <summary>   Compiles the given sources. </summary>
        ///
        /// <remarks>   vasko, 09-Dec-17. </remarks>
        ///
        /// <param name="sources">  A variable-length parameters list containing sources. </param>
        ///
        /// <returns>   An EmitResult. </returns>

        public EmitResult Compile(params string[] sources)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var trees = sources.Select(x => CSharpSyntaxTree.ParseText(x));
            var compilation = CSharpCompilation.Create(_assemblyName,
                syntaxTrees : trees,
                options : options,
                references: References); 
            var result = compilation.Emit(Filepath);
            return result;
        }

        /// <summary>   Compile and get assembly. </summary>
        ///
        /// <remarks>   vasko, 09-Dec-17. </remarks>
        ///
        /// <exception cref="CompilationFailed">    Thrown when a compilation failed error condition
        ///                                         occurs. </exception>
        ///
        /// <param name="sources">  A variable-length parameters list containing sources. </param>
        ///
        /// <returns>   An Assembly. </returns>

        public Assembly CompileAndGetAssembly(params string[] sources)
        {
            var result = Compile(sources);
            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);
                var message = new StringBuilder();
                foreach (Diagnostic diagnostic in failures)
                {
                    message.AppendFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }
                throw new CompilationFailed(message.ToString());
            }
            else
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Filepath);
                return assembly;
            }
        }

    }
}
