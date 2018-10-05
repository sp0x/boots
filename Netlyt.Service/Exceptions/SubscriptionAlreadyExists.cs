using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Exceptions
{
    public class SubscriptionAlreadyExists : Exception
    {
        public SubscriptionAlreadyExists(string forMail) : base("Email: " + forMail)
        {

        }
    }
}
