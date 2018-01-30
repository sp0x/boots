using System;
using System.Collections.Concurrent;
using System.Reflection;
using nvoid.db.Caching;

namespace Netlyt.Service.Donut
{
    public class CacheSetSource : ICacheSetSource
    {
        private static readonly MethodInfo _genericCreate
            = typeof(CacheSetSource).GetTypeInfo().GetDeclaredMethod(nameof(CreateConstructor));

        private readonly ConcurrentDictionary<Type, Func<ICacheSetCollection, object>> _cache
            = new ConcurrentDictionary<Type, Func<ICacheSetCollection, object>>();

        public virtual ICacheSet Create(ICacheSetCollection context, Type type)
        {
            var result = _cache.GetOrAdd(
                type,
                t => (Func<ICacheSetCollection, object>)_genericCreate.MakeGenericMethod(t).Invoke(null, null))(context);
            return result as ICacheSet;
        }
         
        private static Func<ICacheSetCollection, object> CreateConstructor<TEntity>()
            where TEntity : class
            => c => new InternalCacheSet<TEntity>(c);
    }
}