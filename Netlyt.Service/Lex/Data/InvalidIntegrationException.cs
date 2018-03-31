using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Lex.Data
{
    public class InvalidIntegrationException : Exception
    {
        public InvalidIntegrationException(string message) : base(message)
        {

        }
    }
}
