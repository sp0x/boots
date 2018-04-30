using System.Collections.Generic;
using Netlyt.Interfaces;

namespace Donut.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class Rule
    {
        public long Id { get; set; }
        public string RuleName { get; set; }
        public string Type { get; set; }
        public List<Donut.Models.ModelRule> Models { get; set; }
        public User Owner { get; set; }
        public bool IsActive { get; set; }

    }
}