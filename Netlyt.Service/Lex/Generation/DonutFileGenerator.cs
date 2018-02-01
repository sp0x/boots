using System;
using System.IO;
using System.Reflection;
using nvoid.db.Caching;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration;

namespace Netlyt.Service.Lex.Generation
{
    public class DonutfileGenerator<TDonut, TContext>
        where TContext : DonutContext
        where TDonut : Donutfile<TContext>
    {
        private string _template;
        private RedisCacher _cacher;
        private DataIntegration _integration;
        private Type _tContext;

        public DonutfileGenerator(DataIntegration integration, RedisCacher cacher)
        {
            _tContext = typeof(TContext);
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
        public TDonut Generate()
        { 
            var tobj = Activator.CreateInstance(typeof(TDonut), new[] { _cacher }) as TDonut;
            var context = Activator.CreateInstance(_tContext, new object[]{ _cacher, _integration });
            tobj.Context = context as TContext; 
            return tobj;
        }

        public void WithContext<T>() where T : TContext
        {
            _tContext = typeof(T);
        }
    }
}