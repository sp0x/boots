using System.Collections.Generic;

namespace Netlyt.Service.Lex.Expressions
{
    public interface IExpression
    {
        IExpression Parent { get; set; } 
        IEnumerable<IExpression> GetChildren();
    }
}