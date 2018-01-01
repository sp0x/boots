using System.Collections.Generic;
using nvoid.db.DB;

namespace Netlyt.Service
{
    public class Organization : Entity
    {
        public List<User> Members { get; set; }
        public string Name { get; set; }
    }
}