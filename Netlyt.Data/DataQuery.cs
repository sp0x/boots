using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Netlyt.Data
{
    public class DataQuery
    {
        public IList<Expression> Expressions { get; private set; }

        public DataQuery()
        {
            Expressions = new List<Expression>();
        }

        public DataQuery Add(Expression exp)
        {
            this.Expressions.Add(exp);
            return this;
        }

        public Expression<Func<TRecord, bool>> Compile<TRecord>()
        {
            //            var output = Expression<Func<TRecord, bool>>.MemberBind();
            //
            //            var exp = Expression.Field(null, "");
            //            return exp;
            return null;
        }
    }
}