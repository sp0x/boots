using System.Collections.Generic;
using Donut;
using Netlyt.Interfaces;
using Netlyt.Service.Lex.Parsing;

namespace Netlyt.Service.Lex.Expressions
{
    /// <summary>   An order by expression. </summary>
    ///
    /// <remarks>   Vasko, 05-Dec-17. </remarks>

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
