using System.Collections.Generic;
using nvoid.db.DB;

namespace Netlyt.Web.Models.DataModels
{
    public class Rule: Entity
    {
        public long ID { get; set; }
        public string RuleName { get; set; }
        public string Type { get; set; }
        public List<Model> Models { get; set; }
        public User Owner { get; set; }
        public bool IsActive { get; set; }

    }
}