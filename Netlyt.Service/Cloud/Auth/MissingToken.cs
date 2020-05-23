using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Cloud.Auth
{
    public class MissingToken : Exception
    {
        public MissingToken(string message) : base(message)
        {

        }
    }
}
