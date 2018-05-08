using Donut.Lex.Expressions;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IDonutTemplateFunction
    {

    }
    public interface IDonutTemplateFunction<T> : IDonutTemplateFunction, IDonutFunction
    {
        T GetTemplate(CallExpression exp, DonutCodeContext context);
    }
}