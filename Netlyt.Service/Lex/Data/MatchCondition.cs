using System.Collections.Generic;

namespace Netlyt.Service.Lex.Data
{
    public class MatchCondition
    {
        public DslObject Object { get; set; }
        public DslOperator Operator { get; set; }
        public string Value { get; set; }
        public List<string> Values { get; set; }

        public DslLogicalOperator LogOpToNextCondition { get; set; }
    }
}