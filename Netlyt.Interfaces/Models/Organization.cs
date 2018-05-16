using System.Collections.Generic;

namespace Netlyt.Interfaces.Models
{
    public class Organization 
    {
        public long Id { get; set; }
        public virtual ICollection<User> Members { get; set; }
        public virtual ApiAuth ApiKey { get; set; }
        public string Name { get; set; }

        public Organization()
        {
            Members = new HashSet<User>();
        }
    }
}