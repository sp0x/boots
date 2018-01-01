using System.Collections.Generic;
using nvoid.db.DB;
using Netlyt.Service.Ml;

namespace Netlyt.Service
{
    public class Rule
        : Entity
    {
        public long Id { get; set; }
        public string RuleName { get; set; }
        public string Type { get; set; }
        public List<Model> Models { get; set; }
        public User Owner { get; set; }
        public bool IsActive { get; set; }

    }
}