using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Netlyt.Service.Lex.Data;


namespace Netlyt.Service.Build
{
    /// <summary>   A c# assembly builder. </summary>
    ///
    /// <remarks>   Vasko, 09-Dec-17. </remarks>

    public class CsCompiler
    {
        private List<MetadataReference> References { get; set; } 
        private string _assemblyName;
        private string _directory;
        private static Assembly _assembly;

        /// <summary>   The filepath of the output assembly. </summary> 
        public string Filepath
        {
            get
            {
                var filename = $"{AssemblyName}.dll";
                var path = string.IsNullOrEmpty(_directory) ?
                    Path.GetFullPath(filename) :
                    Path.Combine(_directory, filename);
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

        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   vasko, 09-Dec-17. </remarks>
        ///
        /// <param name="assemblyName"> Name of the assembly to generate. </param>

        public CsCompiler(string assemblyName, string directory)
        {
            References = new List<MetadataReference>();
            AddReference(Assembly.Load("System.Runtime").Location);
            AddReference(typeof(object).GetTypeInfo().Assembly.Location);
            AddReference(typeof(Hashtable).GetTypeInfo().Assembly.Location);
            AddReference(typeof(Console).GetTypeInfo().Assembly.Location);
            _directory = directory;
            _assemblyName = assemblyName;
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


        /// <summary>   Compiles the given sources. </summary>
        ///
        /// <remarks>   vasko, 09-Dec-17. </remarks>
        ///
        /// <param name="sources">  A variable-length parameters list containing sources. </param>
        ///
        /// <returns>   An EmitResult. </returns>

        public EmitResultAssembly Compile(params string[] sources)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var trees = sources.Select(x => CSharpSyntaxTree.ParseText(x));
            var compilation = CSharpCompilation.Create(_assemblyName,
                syntaxTrees : trees,
                options : options,
                references: References);
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
            if (File.Exists(Filepath))
            {
                File.Delete(Filepath);
            }
            var result = compilation.Emit(Filepath); 
            return new EmitResultAssembly()
            {
                AssemblySymbol = compilation.Assembly,
                FilePath = Filepath,
                Result = result
            };
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
            if (!result.Result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Result.Diagnostics.Where(diagnostic =>
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
                return result.GetAssembly();
            }
        }

        public string GenerateProjectDefinition(string assemblyName, DonutScript script)
        { 
            using (var template = new StreamReader(GetTemplate("Project.txt")))
            {
                var content = template.ReadToEnd();
                var referenceBuilder = new StringBuilder();
                foreach (var reference in References)
                {
                    var referenceTemplate = $"<PackageReference Incude=\"{reference.Display}\" />\n";
                    referenceBuilder.Append(referenceTemplate);
                    //<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="2.0.1" />
                }

                content = content.Replace("$References", referenceBuilder.ToString());
                return content;
            }
        }

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
            if (_assembly == null) _assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Netlyt.Service.Build.Templates.{name}";
            Stream stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new Exception("Template not found!");
            }
            //StreamReader reader = new StreamReader(stream);
            return stream;
        }
    }
}
