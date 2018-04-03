using System.Collections.Generic;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Donut
{
    public interface IDonutFunction
    {
        IDonutFunction Clone();
        List<ParameterExpression> Parameters { get; set; }
        bool IsAggregate { get; set; }
        DonutFunctionType Type { get; set; }
        string Content { get; set; }
        string Name { get; set; }
        string Body { get; set; }
        string Projection { get; set; }
        string GroupValue { get; set; }
        string GetAggregateValue();
        string GetValue();
    }
}