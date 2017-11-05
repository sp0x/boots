using System;
using System.Collections.Generic;
using System.Text;

namespace Peeralize.Service.Lex.Data
{
    public class DslFeatureModel
    {
        public DslFeatureModel()
        {
            Filters = new List<MatchCondition>();
        }

        public FeatureTypeModel Type { get; set; }
        public IList<MatchCondition> Filters { get; set; }
    }
}
