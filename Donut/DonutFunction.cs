using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using Netlyt.Interfaces;

namespace Donut
{
    public class DonutFunction : IDonutFunction
    {
        public string Name { get; set; }
        public bool IsAggregate { get; set; }
        public List<IParameterExpression> Parameters { get; set; }
        public string Body { get; set; }
        public string Projection { get; set; }
        public string GroupValue { get; set; }
        private Expression<Func<BsonValue, object>> _eval;
        private Func<BsonValue, object> _compiledEval;
        public Expression<Func<BsonValue, object>> Eval
        {
            get { return _eval; }
            set
            {
                _eval = value;
                if (_eval != null)
                {
                    _compiledEval = _eval.Compile();
                }
            }
        }
        public DonutFunctionType Type { get;  set; }

        public Expression GetEvalBody()
        {
            if (Eval == null) return null;
            UnaryExpression bd = Eval.Body as UnaryExpression;
            return bd.Operand;
        }

        public LambdaExpression GetEvalLambda()
        {
            if (Eval == null) return null;
            var body = GetEvalBody();
            var paramX = Expression.Parameter(typeof(BsonValue), "x");
            var lambda = Expression.Lambda(body, paramX);
            return lambda;
        }

        public string GetCallCode(string varName)
        {
            if (Eval == null) return null;
            var body = GetEvalBody();
            var paramX = Expression.Parameter(typeof(BsonValue), "x");
            var lambda = Expression.Lambda(body, paramX);
            var outType = body.Type.ToString();
            var output = $"Func<BsonValue, {outType}> {varName} = {lambda};";
            return output;
        }

        /// <summary>
        /// The content of the function
        /// </summary>
        public IDonutFeatureDefinition Content { get; set; }

        public DonutFunction(string nm)
        {
            Name = nm;
        }

        public object EvalValue(BsonValue val)
        {
            return _compiledEval.Invoke(val);
        }
        public IDonutFunction Clone()
        {
            var newFn = Activator.CreateInstance(this.GetType(), new object[]{ Name}) as IDonutFunction;
            newFn.Name = Name;
            newFn.IsAggregate = IsAggregate;
            newFn.Body = Body;
            newFn.Projection = Projection;
            newFn.GroupValue = GroupValue;
            newFn.Content = Content;
            newFn.Type = Type;
            newFn.Eval = Eval;
            return newFn;
        }

        public override string ToString()
        {
            return $"{GetValue()}";
        }

        public string GetValue()
        {
            if (Content!=null && !string.IsNullOrEmpty(Content.GetValue())) return Content.GetValue();
            if (!string.IsNullOrEmpty(Projection)) return Projection;
            if (!string.IsNullOrEmpty(GroupValue)) return GroupValue;
            return null;
        }

        public virtual int GetHashCode()
        {
            var content = GetValue();
            return content.GetHashCode();
        }

        /// <summary>
        /// Gets the aggregate body
        /// </summary>
        /// <returns></returns>
        public string GetAggregateValue()
        {
            if (!string.IsNullOrEmpty(Projection)) return Projection;
            if (!string.IsNullOrEmpty(GroupValue)) return GroupValue;
            return null;
        }

        public static DonutFunction Wrap(IDonutFunction df)
        {
            return df as DonutFunction;
        }
    }
}