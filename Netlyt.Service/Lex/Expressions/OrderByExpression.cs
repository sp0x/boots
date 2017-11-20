using System.Collections.Generic;
using Netlyt.Service.Lex.Parsing;

namespace Netlyt.Service.Lex.Expressions
{
    public class OrderByExpression
        : IExpression
    {
        private List<IExpression> Values { get; set; }

        public OrderByExpression(List<IExpression> tree)
        {
            this.Values = tree;
        }


        public IEnumerable<IExpression> GetChildren()
        {
            return Values;
        }
    }
}
