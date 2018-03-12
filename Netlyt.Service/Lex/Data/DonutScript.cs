using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex.Data
{
    public class DonutScript : Expression
    {
        public DonutScript()
        {
            Filters = new List<MatchCondition>();
            Features = new List<AssignmentExpression>();
            Integrations = new List<string>();
        }
        /// <summary>
        /// 
        /// </summary>
        public ScriptTypeInfo Type { get; set; }
        public IList<MatchCondition> Filters { get; set; }
        public List<AssignmentExpression> Features { get; set; }
        public OrderByExpression StartingOrderBy { get; set; }
        public List<string> Integrations { get; set; }

        public void AddIntegrations(List<string> sourceIntegrations)
        {
            this.Integrations = new List<string>(sourceIntegrations.ToArray());
        }
    }
}
