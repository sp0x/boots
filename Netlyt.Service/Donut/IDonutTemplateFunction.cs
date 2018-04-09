using Netlyt.Service.Lex;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Donut
{
    public interface IDonutTemplateFunction : IDonutFunction
    {
        string GetTemplate(CallExpression exp, DonutCodeContext context);
    }
}