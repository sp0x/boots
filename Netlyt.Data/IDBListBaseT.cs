using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Netlyt.Data
{
    /// <summary>
    /// A Database List abstraction, bound to the given type
    ///  Represents a basic blueprint of what a DBEnumerable object (SqlList, MongoList) can do.
    /// </summary>
    /// <typeparam name="TRecord">The type that will be handled as a record</typeparam>
    public interface IDbListBase<TRecord> 
        : IEnumerable<TRecord>, IDbListBase
        where TRecord : class
    {
        IQueryable<TRecord> AsQueryable1 { get; }
        bool Exists(Expression<Func<TRecord, bool>> predicate, TRecord doc);
        IEnumerable<TRecord> Where(Expression<Func<TRecord, bool>> predicate, int count = 0, int skip = 0);
        IEnumerable<TRecord> Where(DataQuery predicate);

        TRecord FirstOrDefault(Expression<Func<TRecord, bool>> predicate);

        TRecord First(Expression<Func<TRecord, bool>> predicate);


        bool Any(Expression<Func<TRecord, bool>> predicate);

        bool SaveOrUpdate(TRecord entity);
        void Add(TRecord element);
        void AddRange(IEnumerable<TRecord> elements);

        bool SaveOrUpdate<TMember>(TRecord existingDomain, Expression<Func<TRecord, TMember>> memberSelector,
            TMember value); 
        bool SaveOrUpdate(Expression<Func<TRecord, bool>> predicate, TRecord replaceWith); 
        bool Delete(TRecord elem);
        bool DeleteAll(IEnumerable<TRecord> elements, CancellationToken? cancellationToken = null);

        Task<bool> DeleteAllAsync(IEnumerable<TRecord> elements,
            CancellationToken? cancellationToken = null);

        IEnumerable<TRecord> Range(int skip, int limit);
        IEnumerable<TRecord> In<T>(Expression<Func<TRecord, T>> func, IEnumerable<T> values)
            where T : class;
        List<Index> GetIndexes();
    }
}