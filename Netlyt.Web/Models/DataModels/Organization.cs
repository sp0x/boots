using System.Collections.Generic;

namespace Netlyt.Web.Models.DataModels
{
    public class Organization
    {
        public int ID { get; set; }
        public List<User> Members { get; set; }
        public string Name { get; set; }
    }
}