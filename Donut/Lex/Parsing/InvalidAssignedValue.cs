using System;

namespace Donut.Lex.Parsing
{
    public class InvalidAssignedValue : Exception
    {
        public InvalidAssignedValue(string message) : base(message)
        {
        }
    }
}