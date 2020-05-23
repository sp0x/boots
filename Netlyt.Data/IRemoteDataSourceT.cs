using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Netlyt.Data
{
    public interface IRemoteDataSource 
    {
        bool Save(object entity); 
        bool Update(object entity);
        bool Update(object entity, Func<object, bool> filter);
        int Size { get; } 
    } 
    
    public interface IRemoteDataSource<TRecord> : IQueryable<TRecord>, IRemoteDataSource
        where TRecord : class
    {
        object Session { get; }
        bool Connect(Object server);//Networking.IAsyncServer  server);
        bool Connected { get; }
        bool Save(TRecord entity);
        bool Update(TRecord entity);
        bool Update(TRecord entity, Expression<Func<TRecord, bool>> filter);
        IEnumerable<TRecord> Range(int skip, int limit);

        bool SaveOrUpdate<TMember>(TRecord existingEntity, Expression<Func<TRecord, TMember>> memberSelector,
            TMember value);

        bool SaveOrUpdate(Expression<Func<TRecord, bool>> predicate, TRecord replaceWith);
    }
}