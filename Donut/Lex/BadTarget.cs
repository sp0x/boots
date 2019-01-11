using System;
using System.Collections.Generic;
using System.Text;

namespace Donut.Lex
{
    public class BadTarget : Exception
    {
        public BadTarget(string message) : base(message)
        {
        }
    }
}
