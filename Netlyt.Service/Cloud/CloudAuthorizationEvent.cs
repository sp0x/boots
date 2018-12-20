using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Cloud
{
    public class CloudAuthorizationEvent
    {
        public long Id { get; set; }
        public virtual ApiAuth ApiKey { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Token { get; set; }
        public string Name { get; set; }
        public NodeRole Role { get; set; }

    }
}
