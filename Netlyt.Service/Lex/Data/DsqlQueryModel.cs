using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex.Data
{
    public class DslFeatureModel
    {
        public DslFeatureModel()
        {
            Filters = new List<MatchCondition>();
            Features = new List<AssignmentExpression>();
        }

        public FeatureTypeModel Type { get; set; }
        public IList<MatchCondition> Filters { get; set; }
        public List<AssignmentExpression> Features { get; set; }
        public OrderByExpression StartingOrderBy { get; set; }
    }
}
