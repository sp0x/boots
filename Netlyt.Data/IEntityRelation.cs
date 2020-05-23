using System;
using System.Linq.Expressions;

namespace Netlyt.Data
{
    public interface IEntityRelation
    {
        Expression<Func<T, bool>> Compile<T>(Object filterObj) where T : class;
    }

    public interface IEntityRelation<T> : IEntityRelation
    {
        IEntityRelation<T> Or<TValue>(Func<TValue, Expression<Func<T, bool>>> predicate);
        IEntityRelation<T> And<TValue>(Func<TValue, Expression<Func<T, bool>>> predicate);
        Expression<Func<T, bool>> Compile<V>(V filterObj);
        uint Complexion { get; }
    }
}