using System;
using System.IO;
using System.Reflection;
using nvoid.db.Caching;
using Netlyt.Service.Donut;

namespace Netlyt.Service.Lex.Generation
{
    public class DonutfileGenerator<T>
        where T : Donutfile
    {
        private string _template;
        private RedisCacher _cacher;

        public DonutfileGenerator(RedisCacher cacher)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Netlyt.Service.Lex.Templates.Donutfile.txt";
            _cacher = cacher;

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
        public T Generate()
        { 
            var tobj = Activator.CreateInstance(typeof(T), new[] { _cacher }) as T; 
            return tobj;
        }
    }
}