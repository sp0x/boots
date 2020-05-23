using System;
using System.IO;
using System.Reflection;

namespace Netlyt.Service
{
    public class DonutAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
    {
        private string _directory;
        public DonutAssemblyLoadContext(string directory)
        {
            _directory = directory;
        } 

        protected override Assembly Load(AssemblyName assemblyName)
        { 
            
            return LoadFromFolderOrDefault(assemblyName);
        }

        Assembly LoadFromFolderOrDefault(AssemblyName assemblyName)
        {
            try
            {
                var path = Path.Combine(_directory, assemblyName.Name);

                if (File.Exists(path + ".dll"))
                    return LoadFromAssemblyPath(path + ".dll");

                if (File.Exists(path + ".exe"))
                    return LoadFromAssemblyPath(path + ".exe");

                //TODO: Probably missing something here. What if it's
                //             a transitive nuget dependency, not literally in the
                //             test project's build output folder?

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }
    }
}