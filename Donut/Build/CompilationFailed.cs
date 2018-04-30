using System;

namespace Donut.Build
{
    public class CompilationFailed
        : Exception
    {
        public CompilationFailed(string message) : base(message)
        {
        }
    }
}
