using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Cloud
{
    public class CloudAuthorizationEvent
    {
        public long Id { get; set; }
        public virtual ApiAuth ApiKey { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Token { get; set; }
        public string Name { get; set; }
        public NodeRole Role { get; set; }

    }
}
