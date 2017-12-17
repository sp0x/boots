using System.Collections.Generic;

namespace Netlyt.Service.Lex.Expressions
{
    public class BlockExpression
        : Expression
    {
        public List<IExpression> Children { get; set; }

        public BlockExpression()
        {
            Children = new List<IExpression>();
        }

        public override IEnumerable<IExpression> GetChildren()
        {
            return Children;
        }
    }
}