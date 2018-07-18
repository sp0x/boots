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
        public IEnumerable<ModelTarget> Targets { get; set; }
        public string AssemblyPath { get; set; }

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
                output += "target " + string.Join(", ", Targets.Select(z=> z.ToDonutScript())) + "\n";
            }
            return output;
        }

        public class Factory
        {

            public static DonutScript Create(string donutName, IEnumerable<ModelTarget> targets, DataIntegration integration)
            {
                var script = new DonutScript() { Type = new ScriptTypeInfo(donutName), Targets = targets };
                ValidateIntegrations(integration);
                script.AddIntegrations(integration);
                script.Targets = targets;
                ValidateIntegrations(integration);
                return script;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="donutName"></param>
            /// <param name="target"></param>
            /// <param name="integration"></param>
            /// <param name="featureBodies"></param>
            /// <returns></returns>
            public static DonutScript CreateWithFeatures(string donutName, IEnumerable<ModelTarget> targets, DataIntegration integration, params string[] featureBodies)
            {
                var script = Create(donutName, targets, integration);
                ValidateIntegrations(integration);
                var tokenizer = new FeatureToolsTokenizer(integration);
                int i = 0;
                foreach (var fstring in featureBodies)
                {
                    if (string.IsNullOrEmpty(fstring)) continue;
                    var featureName = $"f_{i}";
                    try
                    {
                        var parser = new DonutSyntaxReader(tokenizer.Tokenize(fstring));
                        IExpression expFeatureBody = parser.ReadExpression();
                        if (expFeatureBody == null) continue;
                        if (targets!=null && targets.Any(x=>x.Column.Name==expFeatureBody.ToString()))
                        {
                            featureName = targets.First().Column.Name;
                        }
                        var expFeature = new AssignmentExpression(new NameExpression(featureName), expFeatureBody);
                        script.Features.Add(expFeature);
                    }
                    catch (Exception ex)
                    {
                        throw new FeatureGenerationFailed(featureName, fstring, ex);
                    }
                    i++;
                }
                return script;
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
