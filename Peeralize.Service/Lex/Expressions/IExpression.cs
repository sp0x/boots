using System.Collections.Generic;

namespace Peeralize.Service.Lex.Expressions
{
    public interface IExpression
    {
        IEnumerable<IExpression> GetChildren();
    }
}