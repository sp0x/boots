using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace Netlyt.Service.Build
{
    public class EmitResultAssembly
    {
        private Assembly _assembly;
        public EmitResult Result { get; set; }
        public IAssemblySymbol AssemblySymbol { get; set; }
        public string FilePath { get; set; }

        public Assembly GetAssembly()
        {
            if (_assembly != null) return _assembly;
            _assembly = Assembly.LoadFile(FilePath);
            return _assembly;
        }
    }
}