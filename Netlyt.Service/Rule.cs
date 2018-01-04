using System.Collections.Generic;
using nvoid.db.DB;
using Netlyt.Service.Ml;

namespace Netlyt.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class Rule
        : Entity
    {
        public long Id { get; set; }
        public string RuleName { get; set; }
        public string Type { get; set; }
        public List<ModelRule> Models { get; set; }
        public User Owner { get; set; }
        public bool IsActive { get; set; }

    }
}