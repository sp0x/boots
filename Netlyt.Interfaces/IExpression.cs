using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    public interface IExpression
    {
        IExpression Parent { get; set; } 
        IEnumerable<IExpression> GetChildren();
    }
}