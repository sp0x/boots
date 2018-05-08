using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    /// <summary>
    /// Represents a [symbol] = [value] expression.
    /// </summary>
    public class AssignmentExpression
        : Expression
    {
        public NameExpression Member { get; private set; }
        public IExpression Value { get; private set; }

        public AssignmentExpression(NameExpression memberExpression, IExpression valueExpression)
            : base()
        {
            this.Member = memberExpression;
            this.Value = valueExpression;
        } 
        public override string ToString()
        {
            var left = Member.ToString();
            var right = Value.ToString();
            return $"{left} = {right}";
        }
    }
}
