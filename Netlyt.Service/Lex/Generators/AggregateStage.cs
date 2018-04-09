using System.Collections.Generic;
using System.Linq;
using nvoid.db.Extensions;
using Netlyt.Service.Donut;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Lex.Generators
{
    public class AggregateStage
    {
        private DonutScript _script;
        public AggregateStageType Type { get; set; }
        public IDonutFunction Function { get; set; }
        public List<AggregateStage> Children { get; set; }

        public AggregateStage(DonutScript script, IDonutFunction function)
        {
            Function = function;
            _script = script;
            Children = new List<AggregateStage>();//new AggregateJobTree(script);
            switch (function.Type)
            {
                case DonutFunctionType.Project: Type = AggregateStageType.Project; break;
                case DonutFunctionType.Group: Type = AggregateStageType.Group; break;
            }
        }
        public IEnumerable<AggregateStage> GetGroupings()
        {
            return Children.Where(x => x.Type == AggregateStageType.Group);
        }
        public IEnumerable<AggregateStage> GetProjections()
        {
            return Children.Where(x => x.Type == AggregateStageType.Project);
        }
        public IEnumerable<AggregateStage> GetFilters()
        {
            return Children.Where(x => x.Type == AggregateStageType.Match);
        }
        public AggregateStage Clone()
        {
            var nstage = new AggregateStage(_script, Function);
            nstage.Type = Type;
            nstage.Children = new List<AggregateStage>(Children.ToArray());
            return nstage;
        }

        public override string ToString()
        {
            return $"{Type}: {Function}";
        }

        /// <summary>
        /// Evaluates the value of this stage and it's substages
        /// </summary>
        /// <returns></returns>
        public string GetValue()
        {
            var template = Function.GetValue();
            var lstParameters = Function.Parameters.ToList();
            for (int i = 0; i < lstParameters.Count; i++)
            {
                var parameter = lstParameters[i];
                object paramOutputObj;
                var pValue = parameter.Value;
                var argExpVisitor = new DonutFeatureGeneratingExpressionVisitor(_script);
                //If we visit functions here, note that in the pipeline
                var paramStr = argExpVisitor.Visit(parameter, out paramOutputObj);
                var subAggregateTree = argExpVisitor.AggregateTree;
                Children.AddRange(subAggregateTree.Stages);
                if (pValue is CallExpression || paramOutputObj is IDonutTemplateFunction)
                {
                    if (Function.Type == DonutFunctionType.Group)
                    {
                        if (paramOutputObj is IDonutTemplateFunction iDonutFn &&
                            iDonutFn.Type == DonutFunctionType.Project)
                        {
                            throw new System.Exception(
                                $"Function {Function} is a grouping and cannot have projection arguments.");
                        }
                    }
                    template = template.Replace("\"{" + i + "}\"", paramStr);
                }
                else
                {
                    template = template.Replace("{" + i + "}", $"${paramStr}");
                }
            }
            return template;
        }

    }
}