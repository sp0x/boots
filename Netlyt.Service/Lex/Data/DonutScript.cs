using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Lex.Expressions;
using Netlyt.Service.Lex.Parsing;
using Netlyt.Service.Lex.Parsing.Tokenizers;

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

        public void AddIntegrations(params string[] sourceIntegrations)
        {
            if (this.Integrations == null)
            {
                this.Integrations = new List<string>(sourceIntegrations);
            }
            else
            {
                this.Integrations.AddRange(sourceIntegrations);
            }
        }

        public class Factory
        {
            public static DonutScript CreateWithFeatures(string donutName, params string[] featureBodies)
            {
                var ds = new DonutScript();
                ds.Type = new ScriptTypeInfo()
                {
                    Name = donutName
                };
                var tokenizer = new PrecedenceTokenizer();
                int i = 0;
                foreach (var fstring in featureBodies)
                {
                    if (string.IsNullOrEmpty(fstring)) continue;
                    var parser = new TokenParser(tokenizer.Tokenize(fstring));
                    IExpression expFeatureBody = parser.ReadExpression();
                    if (expFeatureBody == null) continue;
                    var expFeature = new AssignmentExpression(new VariableExpression($"f_{i}"), expFeatureBody);
                    ds.Features.Add(expFeature);
                    i++;
                }
                return ds;
            }
        }
    }
}
