using Donut.Lex.Expressions;
using Donut.Lex.Generation;

namespace Donut.Lex
{
    public class MapReduceJsScript
    {
        public string Map { get; private set; }
        public string Reduce { get; private set; }

        public static MapReduceJsScript Create(MapReduceExpression mapReduce)
        {
            var keyGenerator = mapReduce.GetCodeGenerator();
            var aggGenerator = mapReduce.Aggregate.GetCodeGenerator();
            var keyValue = keyGenerator.GenerateFromExpression(mapReduce);
            var aggValue = aggGenerator.GenerateFromExpression(mapReduce.Aggregate);
            var sc = new MapReduceJsScript()
            {
                Reduce = aggValue,
                Map = keyValue
            };
            return sc;
        }
    }
}