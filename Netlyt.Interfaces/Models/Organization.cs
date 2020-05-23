using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Donut.Interfaces.Models;

namespace Netlyt.Interfaces.Models
{
    public class Organization 
    {
        public long Id { get; set; }
        public virtual ICollection<User> Members { get; set; }
        [ForeignKey("ApiKey")]
        public long ApiKeyId { get; set; }
        public virtual ApiAuth ApiKey { get; set; }
        public string Name { get; set; }

        public Organization()
        {
            Members = new HashSet<User>();
            ApiKey = ApiAuth.Generate();
        }
    }
}