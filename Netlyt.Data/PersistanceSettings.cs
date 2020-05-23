using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Netlyt.Data
{
    public class PersistanceSettings// : ConfigurationSection
    {
        //[ConfigurationProperty("socialEntity", IsDefaultCollection = false)]
        //[ConfigurationCollection(typeof(SocialEntitySection), AddItemName = "instance", CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public IList<SocialEntitySetting> SocialEntitySettings { get; set; }

        //[ConfigurationProperty("assemblies", IsDefaultCollection = false)]
        //[ConfigurationCollection(typeof(AssembliesSection), AddItemName = "entry", CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public IList<string> Assemblies { get; set; }// => (AssembliesSection)base["assemblies"];


        //[ConfigurationProperty("fallbackServers", IsDefaultCollection = false)]
        //[ConfigurationCollection(typeof(FallbackServersSection), AddItemName = "entry", CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public IList<string> FallbackServers { get; set; } //=> (FallbackServersSection)base["fallbackServers"];

        //[ConfigurationProperty("entities", IsDefaultCollection = false)]
        //[ConfigurationCollection(typeof(EntitiesSection), AddItemName = "entry", CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public EntitiesConfiguration Entities { get; set; } //=> (EntitiesConfiguration)base["entities"];

        //[ConfigurationProperty("dbs", IsDefaultCollection = false)]
        //[ConfigurationCollection(typeof(DbServersSection), AddItemName = "entry", CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public List<DatabaseConfiguration> DBs { get; set; } //=>(DbServersSection)base["dbs"];
        public CacheConfiguration Cache { get; set; } //=>(DbServersSection)base["dbs"];

        

        //[ConfigurationProperty("users", IsDefaultCollection = false)]
        //[ConfigurationCollection(typeof(DbServersSection), AddItemName = "entry", CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public List<UserEntry> Users { get; set; } //=> (UsersSection)base["users"];

        //[ConfigurationProperty("loadLocalAssemblies", IsDefaultCollection = false)]
        public bool LoadLocalAssemblies { get; set; } //=> (bool) base["loadLocalAssemblies"];

        public static string GetPostgresConnectionString(IConfiguration config)
        {
            string existingConfig = config.GetConnectionString("PostgreSQLConnection");
            string envHost = Environment.GetEnvironmentVariable("PGSQL_HOST");
            string envPort = Environment.GetEnvironmentVariable("PGSQL_PORT");
            string envDb = Environment.GetEnvironmentVariable("PGSQL_DB");
            string envPass = Environment.GetEnvironmentVariable("PGSQL_PASS");
            if (string.IsNullOrEmpty(envPort)) envPort = "5432";
            if (!string.IsNullOrEmpty(envHost))
            {
                var envUrl = $"Server={envHost};Port={envPort};User Id=postgres;Password={envPass};Database={envDb}";
                existingConfig = envUrl;
            }
            return existingConfig;
        }
        public CacheConfiguration GetCacheConfig()
        {
            var envCache = new CacheConfiguration();
            envCache.Host = Environment.GetEnvironmentVariable("REDIS_HOST");
            var strPort = Environment.GetEnvironmentVariable("REDIS_PORT");
            envCache.Port = uint.Parse(string.IsNullOrEmpty(strPort) ? "6379" : strPort);
            if (envCache.Port == 0) envCache.Port = 6379;
            envCache.Arguments = Environment.GetEnvironmentVariable("REDIS_ARGS");
            if (Cache != null && string.IsNullOrEmpty(envCache.Host)) return Cache;
            else
            {
                return envCache;
            }
        }

        public static DatabaseConfiguration GetMongoFromEnv()
        {
            var mhost = Environment.GetEnvironmentVariable("MONGO_HOST");
            if (string.IsNullOrEmpty(mhost)) return null;
            var mport = Environment.GetEnvironmentVariable("MONGO_PORT");
            if (string.IsNullOrEmpty(mport)) mport = "27017";
            var muser = Environment.GetEnvironmentVariable("MONGO_USER");
            var mpass = Environment.GetEnvironmentVariable("MONGO_PASS");
            var mdb = Environment.GetEnvironmentVariable("MONGO_DB");
            if (string.IsNullOrEmpty(mdb)) mdb = "netvoid";
            var mauthsource = Environment.GetEnvironmentVariable("MONGO_AUTHSOURCE");
            var mongo = new DatabaseConfiguration();
            mongo.Name = mhost;
            var mongoUrlBuilder = new MongoUrlBuilder();
            mongoUrlBuilder.Server = new MongoServerAddress(mhost, int.Parse(mport));
            mongoUrlBuilder.DatabaseName = mdb;
            if(!string.IsNullOrEmpty(muser)) mongoUrlBuilder.Username = muser;
            if(!string.IsNullOrEmpty(mpass)) mongoUrlBuilder.Password = mpass;
            if (!string.IsNullOrEmpty(muser) && !string.IsNullOrEmpty(mpass) && string.IsNullOrEmpty(mauthsource))
            {
                mauthsource = "admin";
            } 
            if (!string.IsNullOrEmpty(mauthsource)) mongoUrlBuilder.AuthenticationSource = mauthsource;
            //mongo.Value = "mongodb://netlyt:gsoeghjoijasg43o0jw90e8bjsdfog@mongo.netlyt.io:27017/netvoid?authSource=admin";
            mongo.Value = mongoUrlBuilder.ToMongoUrl().ToString();
            mongo.Role = "general";
            mongo.Type = DatabaseType.MongoDb;
            Console.WriteLine("MongoDB Env: " + mongo.Value);
            return mongo;
        }
    } 
}