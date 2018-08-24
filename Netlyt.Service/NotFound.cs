using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service
{
    public class NotFound : Exception
    {
        public NotFound()
        {

        }

        public NotFound(string message) : base(message)
        {

        }
    }

    public class Forbidden : Exception
    {
        public Forbidden()
        {

        }

        public Forbidden(string message) : base(message)
        {

        }
    }
}
