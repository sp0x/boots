using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Data;
using Donut.Lex.Expressions;
using Donut.Lex.Parsing;
using Donut.Parsing.Tokenizers;
using Netlyt.Interfaces;

namespace Donut.Lex.Data
{
    public class DonutScript : Expression, IDonutScript
    {
        public DonutScript()
        {
            Filters = new List<MatchCondition>();
            Features = new List<AssignmentExpression>();
            Integrations = new HashSet<Donut.Data.DataIntegration>();
        }
        /// <summary>
        /// 
        /// </summary>
        public ScriptTypeInfo Type { get; set; }
        public IList<MatchCondition> Filters { get; set; }
        public List<AssignmentExpression> Features { get; set; }
        public OrderByExpression StartingOrderBy { get; set; }
        public HashSet<Donut.Data.DataIntegration> Integrations { get; set; }
        public ModelTargets Targets { get; set; }

        public void AddIntegrations(params Donut.Data.DataIntegration[] sourceIntegrations)
        {
            if (this.Integrations == null)
            {
                this.Integrations = new HashSet<Donut.Data.DataIntegration>(sourceIntegrations);
            }
            else
            {
                foreach(var ign in sourceIntegrations) this.Integrations.Add(ign);
            }
        }

        public IEnumerable<DatasetMember> GetDatasetMembers()
        {
            var dtSources = Integrations.Where(x => x != null).Skip(1); //Skip the root integration
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

            if (Targets!=null)
            {
                output += "target " + Targets.ToDonutScript() + "\n";
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
            public static DonutScript CreateWithFeatures(string donutName, ModelTargets targets, Donut.Data.DataIntegration integration, params string[] featureBodies)
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
                ds.Targets = targets;
                foreach (var fstring in featureBodies)
                {
                    if (string.IsNullOrEmpty(fstring)) continue;
                    var featureName = $"f_{i}";
                    try
                    {
                        var parser = new DonutSyntaxReader(tokenizer.Tokenize(fstring));
                        IExpression expFeatureBody = parser.ReadExpression();
                        if (expFeatureBody == null) continue;
                        if (targets!=null && targets.Has(expFeatureBody.ToString()))
                        {
                            featureName = targets.Columns.First().Name;
                        }

                        var expFeature = new AssignmentExpression(new NameExpression(featureName), expFeatureBody);
                        ds.Features.Add(expFeature);
                    }
                    catch (Exception ex)
                    {
                        throw new FeatureGenerationFailed(featureName, fstring, ex);
                    }
                    i++;
                }
                return ds;
            }

            private static void ValidateIntegrations(params Donut.Data.DataIntegration[] integrations)
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

        public Donut.Data.DataIntegration GetRootIntegration()
        {
            return Integrations.FirstOrDefault();
        }
    }

    public class FeatureGenerationFailed : Exception
    {
        public FeatureGenerationFailed(string featureName, string featureBody, Exception internalEx) 
            : base($"Could not generate feature: {featureName}\n Body: {featureBody}", internalEx)
        {
        }
    }
}
