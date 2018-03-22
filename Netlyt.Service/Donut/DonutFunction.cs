using System.Collections.Generic;
using System.Linq;
using Netlyt.Service.Lex.Expressions;

namespace Netlyt.Service.Donut
{
    public enum DonutFunctionType
    {
        Standard, Group, Project, GroupKey
    }
    public class DonutFunction
    {
        string Name { get; set; }
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

        public DonutFunction Clone()
        {
            var newFn = new DonutFunction(Name);
            newFn.Name = Name;
            newFn.IsAggregate = IsAggregate;
            newFn.Body = Body;
            newFn.Projection = Projection;
            newFn.GroupValue = GroupValue;
            newFn.Content = Content;
            newFn.Type = Type;
            return newFn;
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