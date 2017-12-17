using System;

namespace Netlyt.Service.Lex.Parsing
{
    public class InvalidAssignedValue : Exception
    {
        public InvalidAssignedValue(string message) : base(message)
        {
        }
    }
}