
using System.Collections.Generic;

namespace Netlyt.Service.Lex.Expressions
{
    /// <summary>
    /// Represents a [symbol] = [value] expression.
    /// </summary>
    public class AssignmentExpression
        : IExpression
    {
        public IExpression Member { get; private set; }
        public IExpression Value { get; private set; }

        public AssignmentExpression(IExpression memberExpression, IExpression valueExpression)
        {
            this.Member = memberExpression;
            this.Value = valueExpression;
        }

        public IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression>();
        }
    }
}
