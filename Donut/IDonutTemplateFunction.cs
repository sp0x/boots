using Donut.Lex.Expressions;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IDonutTemplateFunction : IDonutFunction
    {
        string GetTemplate(CallExpression exp, DonutCodeContext context);
    }
}