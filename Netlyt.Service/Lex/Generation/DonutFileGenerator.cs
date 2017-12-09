using System.IO;
using System.Reflection;

namespace Netlyt.Service.Lex.Generation
{
    public class DonutFileGenerator
    {
        private string _template;
        public DonutFileGenerator()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Netlyt.Service.Lex.Templates.Donutfile.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                _template = reader.ReadToEnd();
            }
        }
    }
}