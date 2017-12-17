using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Build
{
    public class CompilationFailed
        : Exception
    {
        public CompilationFailed(string message) : base(message)
        {
        }
    }
}
