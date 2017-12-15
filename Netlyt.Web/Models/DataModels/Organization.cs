using System.Collections.Generic;
using nvoid.db.DB;

namespace Netlyt.Web.Models.DataModels
{
    public class Organization: Entity
    {
        public int ID { get; set; }
        public List<User> Members { get; set; }
        public string Name { get; set; }
    }
}