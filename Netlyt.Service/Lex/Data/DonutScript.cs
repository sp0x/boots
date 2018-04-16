using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Service.Integration;
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
            Integrations = new HashSet<DataIntegration>();
        }
        /// <summary>
        /// 
        /// </summary>
        public ScriptTypeInfo Type { get; set; }
        public IList<MatchCondition> Filters { get; set; }
        public List<AssignmentExpression> Features { get; set; }
        public OrderByExpression StartingOrderBy { get; set; }
        public HashSet<DataIntegration> Integrations { get; set; }
        public string TargetAttribute { get; set; }

        public void AddIntegrations(params DataIntegration[] sourceIntegrations)
        {
            if (this.Integrations == null)
            {
                this.Integrations = new HashSet<DataIntegration>(sourceIntegrations);
            }
            else
            {
                foreach(var ign in sourceIntegrations) this.Integrations.Add(ign);
            }
        }

        public IEnumerable<DatasetMember> GetDatasetMembers()
        {
            var dtSources = Integrations.Where(x => x != null);//.Skip(1);
            foreach (var source in dtSources)
            {
                yield return new DatasetMember(source);
            }
        }

        public override string ToString()
        {
            var output = $"define {Type.Name}\n";
            var strIntegrations = string.Join(", ", Integrations.Select(x => x.Name).ToArray());
            output += "from " + strIntegrations + Environment.NewLine;
            foreach (var feature in Features)
            {
                var strFtr = $"set {feature.Member} = {feature.Value}\n";
                output += strFtr;
            }

            if (!string.IsNullOrEmpty(TargetAttribute))
            {
                output += "target " + TargetAttribute + "\n";
            }
            return output;
        }

        public class Factory
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="donutName"></param>
            /// <param name="target"></param>
            /// <param name="integration"></param>
            /// <param name="featureBodies"></param>
            /// <returns></returns>
            public static DonutScript CreateWithFeatures(string donutName, string target, DataIntegration integration, params string[] featureBodies)
            {
                var ds = new DonutScript();
                ValidateIntegrations(integration);
                ds.Type = new ScriptTypeInfo()
                {
                    Name = donutName
                };
                var tokenizer = new FeatureToolsTokenizer(integration);
                int i = 0;
                ds.AddIntegrations(integration);
                ds.TargetAttribute = target;
                foreach (var fstring in featureBodies)
                {
                    if (string.IsNullOrEmpty(fstring)) continue;
                    var featureName = $"f_{i}";

                    var parser = new DonutSyntaxReader(tokenizer.Tokenize(fstring));
                    IExpression expFeatureBody = parser.ReadExpression();
                    if (expFeatureBody == null) continue;
                    if (!string.IsNullOrEmpty(target) && expFeatureBody.ToString() == target)
                    {
                        featureName = target;
                    }
                    var expFeature = new AssignmentExpression(new VariableExpression(featureName), expFeatureBody);
                    ds.Features.Add(expFeature);
                    i++;
                }
                return ds;
            }

            private static void ValidateIntegrations(params DataIntegration[] integrations)
            {
                foreach (var intg in integrations)
                {
                    if (string.IsNullOrEmpty(intg.Name))
                        throw new InvalidIntegrationException("Integration name is requered!");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dsName"></param>
        /// <returns></returns>
        public DatasetMember GetDatasetMember(string dsName)
        {
            var dtSources = Integrations.Where(x => x != null);//.Skip(1);
            foreach (var source in dtSources)
            {
                if (source.Name == dsName) return new DatasetMember(source);
            }
            return null;
        }

        public DataIntegration GetRootIntegration()
        {
            return Integrations.FirstOrDefault();
        }
    }
}
