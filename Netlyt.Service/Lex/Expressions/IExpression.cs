using System.Collections.Generic;

namespace Netlyt.Service.Lex.Expressions
{
    public interface IExpression
    {
        IEnumerable<IExpression> GetChildren();
    }
}