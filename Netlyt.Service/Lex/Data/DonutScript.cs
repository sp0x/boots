﻿using System;
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
            Integrations = new List<DataIntegration>();
        }
        /// <summary>
        /// 
        /// </summary>
        public ScriptTypeInfo Type { get; set; }
        public IList<MatchCondition> Filters { get; set; }
        public List<AssignmentExpression> Features { get; set; }
        public OrderByExpression StartingOrderBy { get; set; }
        public List<DataIntegration> Integrations { get; set; }
        public string TargetAttribute { get; set; }

        public void AddIntegrations(params DataIntegration[] sourceIntegrations)
        {
            if (this.Integrations == null)
            {
                this.Integrations = new List<DataIntegration>(sourceIntegrations);
            }
            else
            {
                this.Integrations.AddRange(sourceIntegrations);
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
                foreach (var fstring in featureBodies)
                {
                    if (string.IsNullOrEmpty(fstring)) continue;
                    var parser = new DonutSyntaxReader(tokenizer.Tokenize(fstring));
                    IExpression expFeatureBody = parser.ReadExpression();
                    if (expFeatureBody == null) continue;
                    var featureName = $"f_{i}";
                    if (!string.IsNullOrEmpty(target) && expFeatureBody.ToString() == target)
                    {
                        featureName = target;
                    }
                    var expFeature = new AssignmentExpression(new VariableExpression(featureName), expFeatureBody);
                    ds.Features.Add(expFeature);
                    i++;
                }
                ds.TargetAttribute = target;
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
    }
}
