using System.Collections.Generic;
using Netlyt.Service.Lex.Parsing;

namespace Netlyt.Service.Lex.Expressions
{
    public class OrderByExpression
        : Expression
    {
        public IEnumerable<IExpression> ByClause { get; set; }

        public OrderByExpression(IEnumerable<IExpression> tree)
        {
            this.ByClause = tree;
        }


        public override IEnumerable<IExpression> GetChildren()
        {
            return ByClause;
        }
    }
}
