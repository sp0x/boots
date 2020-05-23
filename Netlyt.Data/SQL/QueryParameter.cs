using System.Linq.Expressions;
using System.Reflection;

namespace Netlyt.Data.SQL
{
    public class QueryParameter
    {
        public MemberExpression MemberExpression { get; private set; }
        public PropertyInfo Member { get; set; } 

        public QueryParameter(MemberExpression left, PropertyInfo member)
        {
            MemberExpression = left;
            Member = member;
        }

        public object GetValue(object entity)
        {
            return Member.GetValue(entity);
        }
    }
}