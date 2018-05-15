using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IDonutFunction
    {
        IDonutFunction Clone();
        List<Donut.Lex.Expressions.ParameterExpression> Parameters { get; set; }
        bool IsAggregate { get; set; }
        DonutFunctionType Type { get; set; }
        Expression<Func<BsonValue, object>> Eval { get; set; }
        IDonutFeatureDefinition Content { get; set; }
        string Name { get; set; }
        string Body { get; set; }
        string Projection { get; set; }
        string GroupValue { get; set; }
        string GetAggregateValue();
        string GetValue();
        int GetHashCode();
        object EvalValue(BsonValue val);
        Expression GetEvalBody();
        LambdaExpression GetEvalLambda();
        string GetCallCode(string varName);
    }
}