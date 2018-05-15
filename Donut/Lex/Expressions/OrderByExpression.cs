using System.Collections.Generic;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    /// <summary>   An order by expression. </summary>
    ///
    /// <remarks>   Vasko, 05-Dec-17. </remarks>

    public class OrderByExpression
        : Expression
    {
        public IEnumerable<IExpression> ByClause { get; set; }

        public OrderByExpression()
        {

        }

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
