using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    public interface IParameterExpression : IExpression
    {
        IExpression Value { get; }
        string ToString();
    }
}