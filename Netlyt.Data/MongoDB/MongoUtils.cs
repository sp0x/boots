using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Donut;

namespace Netlyt.Data.MongoDB
{
    /// <summary>
    /// Module helping with the mapping of different EntryCollection
    /// </summary>
    public static class MongoUtils
    {

        public static List<string> IncludedAssemblies { get; set; } = new List<string>();
        private static List<string> mongoFallbackServers = new List<string>();

        public static object AddMongoFallback(string server)
        {
            if (null==mongoFallbackServers || mongoFallbackServers.Count == 0)
                mongoFallbackServers = new List<string>();
            mongoFallbackServers.Add(server);
            return mongoFallbackServers;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="names">Assembly names to add</param>
        public static void AddAssemblyToMap(params string[] names)
        {
            if (IncludedAssemblies == null)
                IncludedAssemblies = new List<String>();
            IncludedAssemblies.AddRange(names);
        }

        public static void AddMapedAssembly(Assembly asm)
        {
            Type type = typeof(MongoDbClassMap<>);
            if (asm == null) return;
            var classMaps = asm.GetLoadableTypes()
                .Where(t =>
                    t.BaseType != null &&
                    t.BaseType.IsGenericType &&
                    t.BaseType.GetGenericTypeDefinition().ToString() == type.ToString());
            //automate the new *ClassMap()
            foreach (Type classMap in classMaps)
            {
                Activator.CreateInstance(classMap.LoadType());
            }
        }
    }
}