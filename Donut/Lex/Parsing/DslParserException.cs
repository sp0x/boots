using System;

namespace Donut.Lex.Parsing
{
    internal class DslParserException : Exception
    {
        public DslParserException(string s) : base(s)
        {
            
        }
    }
}