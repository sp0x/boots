using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    public interface IParameterExpression
    {
        IExpression Value { get; }

        IEnumerable<IExpression> GetChildren();
        string ToString();
    }
}