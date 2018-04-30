using System.Collections.Generic;

namespace Donut.Lex.Expressions
{
    public class MapAggregateExpression
        : Expression
    {
        public IEnumerable<AssignmentExpression> Values { get; set; }

        public MapAggregateExpression()
        {
            Values = new List<AssignmentExpression>();
        }

        public MapAggregateExpression(IEnumerable<AssignmentExpression> expressions)
        {
            Values = expressions;
        }
    }
}