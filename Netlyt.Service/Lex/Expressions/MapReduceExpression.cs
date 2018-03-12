using System.Collections.Generic;

namespace Netlyt.Service.Lex.Expressions
{
    /// <summary>   A reduce expression. </summary>
    ///
    /// <remarks>   Vasko, 05-Dec-17. </remarks>

    public class MapReduceExpression : Expression
    {
        public IEnumerable<AssignmentExpression> Keys { get; set; }
        public IEnumerable<AssignmentExpression> ValueMembers { get; set; }
        /// <summary>
        /// A collection of values to aggregate
        /// </summary>
        public MapAggregateExpression Aggregate { get; set; }

        public MapReduceExpression()
        {
            Keys = new List<AssignmentExpression>();
            ValueMembers = new List<AssignmentExpression>();
            Aggregate = new MapAggregateExpression();
        }
    }
}