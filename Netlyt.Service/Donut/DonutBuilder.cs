﻿using System;
using System.IO;
using System.Reflection;
using nvoid.db.Caching;
using Netlyt.Service.Integration;

namespace Netlyt.Service.Donut
{
    public interface IDonutBuilder
    {
        IDonutfile Generate();
    }
    public abstract class DonutBuilderFactory
    {

        public static IDonutBuilder Create(Type donutType, Type donutContextType, DataIntegration integration, RedisCacher cacher, IServiceProvider serviceProvider)
        {
            var builderType = typeof(DonutBuilder<,>).MakeGenericType(new Type[] {donutType, donutContextType});
            //DataIntegration integration, RedisCacher cacher, IServiceProvider serviceProvider
            var builderCtor = builderType.GetConstructor(new Type[]
                {typeof(DataIntegration), typeof(RedisCacher), typeof(IServiceProvider)});
            if (builderCtor == null) throw new Exception("DonutBuilder<> has invalid ctor parameters.");
            var builder = Activator.CreateInstance(builderType, integration, cacher, serviceProvider) ;
            return builder as IDonutBuilder;
        }

    }
    /// <summary>
    /// Builds a donut with a given integration
    /// </summary>
    /// <typeparam name="TDonut">The donut type</typeparam>
    /// <typeparam name="TContext">The donut's context type</typeparam>
    public class DonutBuilder<TDonut, TContext> : IDonutBuilder
        where TContext : DonutContext
        where TDonut : Donutfile<TContext>
    {
        private string _template;
        private RedisCacher _cacher;
        private DataIntegration _integration;
        private Type _tContext;
        private IServiceProvider _serviceProvider;

        public DonutBuilder(DataIntegration integration, RedisCacher cacher, IServiceProvider serviceProvider)
        {
            _tContext = typeof(TContext);
            _serviceProvider = serviceProvider;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Netlyt.Service.Lex.Templates.Donutfile.txt";
            _cacher = cacher;
            _integration = integration; 
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                _template = reader.ReadToEnd();
            }
        }


        /// <summary>
        /// Get a reference to the donutfile for an integration
        /// </summary>
        /// <returns></returns>
        public IDonutfile Generate()
        {
            var tobj = Activator.CreateInstance(typeof(TDonut), new object[] { _cacher, _serviceProvider }) as TDonut;
            var context = Activator.CreateInstance(_tContext, new object[] { _cacher, _integration, _serviceProvider });
            tobj.Context = context as TContext;
            return tobj;
        }

        public void WithContext<T>() where T : TContext
        {
            _tContext = typeof(T);
        }
    }
}