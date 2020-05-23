using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Exceptions
{
    public class ObjectAlreadyExists : Exception
    {
        public ObjectAlreadyExists(string message) : base(message)
        {
        }
    }
}
