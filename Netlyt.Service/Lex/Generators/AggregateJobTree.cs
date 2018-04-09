using System.Collections.Generic;
using Netlyt.Service.Donut;
using Netlyt.Service.Lex.Data;

namespace Netlyt.Service.Lex.Generators
{
    public class AggregateJobTree
    {
        private DonutScript _script;
        public List<AggregateStage> Stages { get; set; }

        public AggregateJobTree(DonutScript script)
        {
            _script = script;
            Stages = new List<AggregateStage>();
        }

        public AggregateStage AddGroup(IDonutFunction function)
        {
            var stage = new AggregateStage(_script, function);
            Stages.Add(stage);
            return stage;
        }

        public AggregateStage AddFunction(IDonutFunction function)
        {
            var stage = new AggregateStage(_script, function);
            Stages.Add(stage);
            return stage;
        }

        public AggregateStage AddProjection(IDonutFunction function)
        {
            var stage = new AggregateStage(_script, function);
            Stages.Add(stage);
            return stage;
        }

        public void Clear()
        {
            Stages.Clear();
        }

        public AggregateJobTree Clone()
        {
            var jtree = new AggregateJobTree(_script);
            foreach (var stage in Stages)
            {
                jtree.Stages.Add(stage.Clone());
            }
            return jtree;
        }
    }

    public enum AggregateStageType { Group, Project, Match }
}