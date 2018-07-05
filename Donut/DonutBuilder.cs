using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Donut.Data;
using Donut.Integration;
using Donut.Lex.Data;
using Netlyt.Interfaces;

namespace Donut
{
    /// <summary>
    /// Helps with creating a donut builder for abstract donuts.
    /// </summary>
    public abstract class DonutGeneratorFactory
    {
        private Type _emitterType;

        public static IDonutBuilder Create<TData>(DonutScript script,
            IIntegration integration, IServiceProvider serviceProvider)
        where TData : class, IIntegratedDocument
        {
            if (string.IsNullOrEmpty(script.AssemblyPath)) return null;
            if (!File.Exists(script.AssemblyPath))
            {
                throw new Exception("Donut assembly not found!");
            }
            var asm = Assembly.LoadFrom(script.AssemblyPath);
            var donutTypes = asm.GetTypes();
            var donutType = donutTypes.FirstOrDefault(x => x.IsInstanceOfType(typeof(IDonutfile)));
            var donutContextType = donutTypes.FirstOrDefault(x => x.IsInstanceOfType(typeof(IDonutContext)));
            var featureEmitterType = donutTypes.FirstOrDefault(x => x.IsInstanceOfType(typeof(IDonutFeatureEmitter)));
            var cacher = serviceProvider.GetService(typeof(IRedisCacher)) as IRedisCacher;
            var builder = Create<TData>(donutType, donutContextType, integration, cacher, serviceProvider);
            builder.SetEmitterType(featureEmitterType);
            return builder;
        }

        void SetEmitterType(Type featureEmitterType)
        {
            _emitterType = featureEmitterType;
        }

        Type GetEmitterType()
        {
            return _emitterType;
        }

        public static IDonutBuilder Create<TData>(Type donutType,
            Type donutContextType,
            IIntegration integration, IRedisCacher cacher, IServiceProvider serviceProvider)
        where TData : class, IIntegratedDocument
        {
            var builderType = typeof(DonutBuilder<,,>).MakeGenericType(new Type[] { donutType, donutContextType, typeof(TData) });
            //DataIntegration integration, RedisCacher cacher, IServiceProvider serviceProvider
            var builderCtor = builderType.GetConstructor(new Type[]
                {typeof(Data.DataIntegration), typeof(IRedisCacher), typeof(IServiceProvider)});
            if (builderCtor == null) throw new Exception("DonutBuilder<> has invalid ctor parameters.");
            var builder = Activator.CreateInstance(builderType, integration, cacher, serviceProvider);
            return builder as IDonutBuilder;
        }

    }
    /// <summary>
    /// Builds a donut with a given integration
    /// </summary>
    /// <typeparam name="TDonut">The donut type</typeparam>
    /// <typeparam name="TContext">The donut's context type</typeparam>
    public class DonutBuilder<TDonut, TContext, TData> : IDonutBuilder
        where TContext : DonutContext
        where TDonut : Donutfile<TContext, TData>
        where TData : class, IIntegratedDocument
    {
        private string _template;
        private IRedisCacher _cacher;
        private Data.DataIntegration _integration;
        private Type _tContext;
        private IServiceProvider _serviceProvider;
        public Type DonutType => typeof(TDonut);
        public Type DonutContextType => typeof(TContext);

        public DonutBuilder(DataIntegration integration, IRedisCacher cacher, IServiceProvider serviceProvider)
        {
            _tContext = typeof(TContext);
            _serviceProvider = serviceProvider;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Donut.Lex.Templates.Donutfile.txt";
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