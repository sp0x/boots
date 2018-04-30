using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    public class Organization 
    {
        public long Id { get; set; }
        public virtual ICollection<User> Members { get; set; }
        public ApiAuth ApiKey { get; set; }
        public string Name { get; set; }

        public Organization()
        {
            Members = new HashSet<User>();
        }
    }
}