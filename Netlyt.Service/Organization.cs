using System.Collections.Generic;
using nvoid.db.DB;

namespace Netlyt.Service
{
    public class Organization : Entity
    {
        public long Id { get; set; }
        public virtual ICollection<User> Members { get; set; }
        public string Name { get; set; }

        public Organization()
        {
            Members = new HashSet<User>();
        }
    }
}