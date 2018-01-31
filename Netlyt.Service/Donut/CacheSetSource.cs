using System;
using System.Collections.Concurrent;
using System.Reflection;
using nvoid.db.Caching;

namespace Netlyt.Service.Donut
{
    public class CacheSetSource : ICacheSetSource
    {
        /// <summary>
        /// The method that creates constructors
        /// </summary>
        private static readonly MethodInfo _genericCreate
            = typeof(CacheSetSource).GetTypeInfo().GetDeclaredMethod(nameof(CreateConstructor));
        /// <summary>
        /// A cache of 
        /// </summary>
        private readonly ConcurrentDictionary<Type, Func<ICacheSetCollection, object>> _cache
            = new ConcurrentDictionary<Type, Func<ICacheSetCollection, object>>();

        public virtual ICacheSet Create(ICacheSetCollection context, Type entityType)
        {
            var result = _cache.GetOrAdd(
                entityType,
                t => (Func<ICacheSetCollection, ICacheSet>)_genericCreate.MakeGenericMethod(t).Invoke(null, null))(context);
            return result as ICacheSet;
        }

        /// <summary>
        /// Creates a constructor for a cache set of TEntity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        private static Func<ICacheSetCollection, ICacheSet<TEntity>> CreateConstructor<TEntity>()
            where TEntity : class
        {
            ICacheSet<TEntity> Ret(ICacheSetCollection c) => new InternalCacheSet<TEntity>(c);
            return Ret;
        }
    }
}