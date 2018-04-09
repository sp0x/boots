using System;
using System.Collections.Generic;
using System.Linq;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Donut
{
    public class DonutFunction : IDonutFunction
    {
        public string Name { get; set; }
        public bool IsAggregate { get; set; }
        public List<ParameterExpression> Parameters { get; set; }
        public string Body { get; set; }
        public string Projection { get; set; }
        public string GroupValue { get; set; }

        public DonutFunctionType Type { get;  set; }

        /// <summary>
        /// The content of the function
        /// </summary>
        public string Content { get; set; }

        public DonutFunction(string nm)
        {
            Name = nm;
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
            return newFn;
        }

        public override string ToString()
        {
            return $"{GetValue()}";
        }

        public string GetValue()
        {
            if (!string.IsNullOrEmpty(Content)) return Content;
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
    }
}