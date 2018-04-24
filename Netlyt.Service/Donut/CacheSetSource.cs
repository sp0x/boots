using System;
using System.Collections.Concurrent;
using System.Reflection;
using nvoid.db;
using nvoid.db.Caching;
using nvoid.db.DB;
using Netlyt.Interfaces;

namespace Netlyt.Service.Donut
{
    /// <summary>
    /// 
    /// </summary>
    public class CacheSetSource : ICacheSetSource
    {
        /// <summary>
        /// The method that creates constructors
        /// </summary>
        private static readonly MethodInfo _genericCreate
            = typeof(CacheSetSource).GetTypeInfo().GetDeclaredMethod(nameof(CreateConstructor));

        /// <summary>
        /// The method that creates constructors
        /// </summary>
        private static readonly MethodInfo _genericCreateData
            = typeof(CacheSetSource).GetTypeInfo().GetDeclaredMethod(nameof(CreateDataSetConstructor));

        /// <summary>
        /// A cache of 
        /// </summary>
        private readonly ConcurrentDictionary<Type, Func<ISetCollection, object>> _cache
            = new ConcurrentDictionary<Type, Func<ISetCollection, object>>();

        /// <summary>
        /// A cache of 
        /// </summary>
        private readonly ConcurrentDictionary<Type, Func<ISetCollection, object>> _cacheData
            = new ConcurrentDictionary<Type, Func<ISetCollection, object>>();

        public virtual ICacheSet Create(ISetCollection context, Type entityType)
        {
            var result = _cache.GetOrAdd(
                entityType,
                t => (Func<ISetCollection, ICacheSet>)_genericCreate.MakeGenericMethod(t).Invoke(null, null))(context);
            return result as ICacheSet;
        }

        public IDataSet CreateDataSet(ISetCollection context, Type entityType)
        {
            var result = _cacheData.GetOrAdd(
                entityType,
                t => (Func<ISetCollection, IDataSet>)_genericCreateData.MakeGenericMethod(t).Invoke(null, null))(context);
            return result as IDataSet;
        }

        /// <summary>
        /// Creates a constructor for a cache set of TEntity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        private static Func<ISetCollection, ICacheSet<TEntity>> CreateConstructor<TEntity>()
            where TEntity : class
        {
            ICacheSet<TEntity> Ret(ISetCollection c) => new InternalCacheSet<TEntity>(c);
            return Ret;
        }

        /// <summary>
        /// Creates a constructor for a cache set of TEntity
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        private static Func<ISetCollection, IDataSet<TEntity>> CreateDataSetConstructor<TEntity>()
            where TEntity : class
        {
            IDataSet<TEntity> Ret(ISetCollection c) => new InternalDataSet<TEntity>(c);
            return Ret;
        }
    }
}