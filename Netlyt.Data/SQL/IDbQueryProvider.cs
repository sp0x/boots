using System;
using System.Linq.Expressions;

namespace Netlyt.Data.SQL
{
    public interface IDbQueryProvider
    {
        Expression<Func<TRecord, bool>> GetUniqueQuery<TRecord>(IDbListBase<TRecord> source, TRecord entity) where TRecord : class;
        Expression<Func<TRecord, bool>> GetUniqueQueryForProperty<TRecord>(
            IRemoteDataSource<TRecord> source,
            string uniqueIndexName,
            TRecord value,
            bool ignoreNullValues = false) where TRecord : class;
        Expression<Func<TRecord, bool>> GetCachedQuery<TRecord>(Object value, string prefix = null)
            where TRecord : class;
        Expression<Func<TRecord, bool>> GetCachedQuery<TRecord>(string key, Object value, string prefix)
            where TRecord : class;
        Expression<Func<TRecord, bool>> GetMemberQuery<TRecord>(object valueToMatch, string elementName = null)
            where TRecord : class;
    }
}