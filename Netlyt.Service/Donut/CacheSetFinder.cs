using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using nvoid.db;
using nvoid.db.Caching;
using Extensions = nvoid.db.Extensions.Extensions;

namespace Netlyt.Service.Donut
{
    public class CacheSetFinder : ICacheSetFinder
    {
        private readonly ConcurrentDictionary<Type, IReadOnlyList<CacheSetProperty>> _cache
            = new ConcurrentDictionary<Type, IReadOnlyList<CacheSetProperty>>();

        private readonly ConcurrentDictionary<Type, IReadOnlyList<DataSetProperty>> _dataSetCache
            = new ConcurrentDictionary<Type, IReadOnlyList<DataSetProperty>>();

        public virtual IReadOnlyList<CacheSetProperty> FindSets(DonutContext context)
            => _cache.GetOrAdd(context.GetType(), FindSets);

        public IReadOnlyList<DataSetProperty> FindDataSets(DonutContext context)
            => _dataSetCache.GetOrAdd(context.GetType(), FindDataSets);

        private static DataSetProperty[] FindDataSets(Type contextType)
        {
            var factory = new ClrPropertySetterFactory();
            return contextType.GetRuntimeProperties()
                .Where(
                    p => !Extensions.IsStatic(p)
                         && !p.GetIndexParameters().Any()
                         && p.DeclaringType != typeof(DonutContext)
                         && p.PropertyType.GetTypeInfo().IsGenericType
                         && p.PropertyType.GetGenericTypeDefinition() == typeof(DataSet<>))
                .OrderBy(p => p.Name)
                .Select(
                    p => new DataSetProperty(
                        p.Name,
                        p.PropertyType.GetTypeInfo().GenericTypeArguments.Single(),
                        p.SetMethod == null ? null : factory.Create(p))
                    {
                        Attributes = p.GetCustomAttributes()
                    })
                .ToArray();
        }

        private static CacheSetProperty[] FindSets(Type contextType)
        {
            var factory = new ClrPropertySetterFactory();

            return contextType.GetRuntimeProperties()
                .Where(
                    p => !Extensions.IsStatic(p)
                         && !p.GetIndexParameters().Any()
                         && p.DeclaringType != typeof(DonutContext)
                         && p.PropertyType.GetTypeInfo().IsGenericType
                         && p.PropertyType.GetGenericTypeDefinition() == typeof(CacheSet<>))
                .OrderBy(p => p.Name)
                .Select(
                    p => new CacheSetProperty(
                        p.Name,
                        p.PropertyType.GetTypeInfo().GenericTypeArguments.Single(),
                        p.SetMethod == null ? null : factory.Create(p))
                    {
                        Attributes = p.GetCustomAttributes()
                    })
                .ToArray();
        }
    }
}