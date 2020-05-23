using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Donut;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Netlyt.Data
{
    /// <summary>
    /// Database configuration
    /// </summary>
    public class DBConfig
    {
        private static string mDefaultHost;
        ///private static System.Configuration.Configuration mConfig;
        public static string SQLUser = "root";
        public static string SQLPass = "012508";
        public static string SQLHost = "127.0.0.1";
        /// <summary>
        /// The current database name.
        /// </summary>
        public static string DatabaseName = "netvoid";
        // Public SQLC As New SqlDb.SQL.SqlCli(User, Pass, Host, SqlDb)
        private static Dictionary<Type, IEntityRelation> mEntityRelations;

        public static bool IsConnectionActive = false;
        //private static CacheConfiguration _cacheConfig;
        private static ConnectionMultiplexer _redisConnection;
        private static DBConfig _instance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configRoot">Custom configuration to use</param>
        private static DBConfig Initialize(IConfiguration configRoot = null)
        {
            var dbConfig = DBConfig.GetInstance();
            mEntityRelations = new Dictionary<Type, IEntityRelation>();
            TypeBase = new Dictionary<Type, IDbListBase>();
            mEntityRelations = new Dictionary<Type, IEntityRelation>();
            IConfiguration config = configRoot;
            if (config == null)
            {
                //Use appsettings and environment variables as overrides
                var builder = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: true)
                   .AddEnvironmentVariables();
                config = builder.Build();
            }
            try
            {
                dbConfig.LoadApplicationConfiguration(config);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Could not read application configuration.\n Error: {0}", ex.Message));
            }
            _instance = dbConfig;
            return dbConfig;
        }

        private DBConfig()
        {
            Databases = new HashSet<DatabaseConfiguration>();
        }

        //internal static System.Configuration.Configuration Configuration => mConfig;

        //public static System.Configuration.Configuration LoadConfiguration()
        //{
        //    return Configuration;
        //} 

        public static string DefaultMongoHost
        {
            get { return mDefaultHost ?? "mongodb://127.0.0.1:27017"; }
            set
            {
                mDefaultHost = value;
            }
        }

        /// <summary>
        /// Local type register, for DataBase list abstractions
        /// </summary>
        public static Dictionary<Type, IDbListBase> TypeBase { get; set; }

        public HashSet<DatabaseConfiguration> Databases { get; private set; }

        public static List<UserEntry> Users { get; set; }
        public static IList<SocialEntitySetting> SocialEntitySettings { get; set; }


        public static String GetConnectionString()
        {
            return string.Format("server={0};user id={1};password={2};database={3};CharSet=utf8", SQLHost, SQLUser, SQLPass, DatabaseName);
        }

        /// <summary>
        /// Get a custom connection string
        /// </summary>
        /// <param name="db"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static string GetConnectionString(string db, string endpoint, string user = null, string password = null)
        {
            db = string.IsNullOrEmpty(db) ? db : db;
            endpoint = string.IsNullOrEmpty(endpoint) ? SQLHost : endpoint;
            user = string.IsNullOrEmpty(user) ? SQLUser : user;
            password = string.IsNullOrEmpty(password) ? SQLPass : password;

            return string.Format("server={0};user id={1};password={2};database={3};CharSet=utf8", endpoint, user, password, db);
        }

        /// <summary>
        /// Registers custom serializers.
        /// </summary>
        private static void SetupSerializers()
        {
            // register your custom map
            BsonSerializer.RegisterSerializer(typeof(Type), new TypeSerializer());
            BsonSerializer.RegisterSerializer(typeof(Lazy<string>), new LazyStringSerializer());
            BsonSerializer.RegisterSerializer(typeof(Lazy<BsonDocument>), new LazyBsonDocumentSerializer());
            var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

        }

        /// <summary>
        /// Loads the application/site's configuration section.
        /// </summary>
        /// <param name="config"></param>
        public void LoadApplicationConfiguration(IConfiguration configuration)
        {
            SetupSerializers(); 
            PersistanceSettings persistanceSettings = new PersistanceSettings();
            configuration.GetSection("persistance").Bind(persistanceSettings);
            var envMongoHost = nvoid.db.DB.Configuration.PersistanceSettings.GetMongoFromEnv();
            if (persistanceSettings != null && envMongoHost!=null)
            {
                persistanceSettings.DBs = new List<DatabaseConfiguration>(new DatabaseConfiguration[]{envMongoHost});
            }
            var lsDataTypes = new List<Type>();
            
            DBConfig.SocialEntitySettings = persistanceSettings.SocialEntitySettings;
            var fallbacks = persistanceSettings.FallbackServers;
            var assemblies = persistanceSettings.Assemblies;
            var dbConfigs = persistanceSettings.DBs;
            this.PersistanceSettings = persistanceSettings;
            //SetCacheConfig(persistanceSettings.Cache);
            EntitiesConfiguration entityConfiguration = persistanceSettings.Entities;
            var lsAssemblies = new List<String>();
            if (fallbacks != null)
            {
                foreach (var fallbackServer in fallbacks) MongoUtils.AddMongoFallback(fallbackServer.ToString());
            }
            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                    lsAssemblies.Add(assembly);
            }

            MongoUtils.AddAssemblyToMap(lsAssemblies.ToArray());

            //Read the users
            if (Users == null) Users = new List<UserEntry>();
            if (persistanceSettings.Users != null)
            {
                foreach (UserEntry u in persistanceSettings.Users) Users.Add(u);
            }
            if (entityConfiguration != null && entityConfiguration.Entities != null)
            {
                foreach (EntityConfiguration entity in entityConfiguration.Entities)
                {
                    Type tmpType = GetEntityType(entity, entityConfiguration);
                    if (tmpType != null)
                    {
                        lsDataTypes.Add(tmpType);
                    }
                    else
                    {
                        throw new Exception("Could not find or load the listed type " + entity.Entity);
                    }
                }
            }
            if (dbConfigs != null)
            {
                //Create the databases
                foreach (DatabaseConfiguration dbConfig in dbConfigs)
                {
                    //Create the new database, for all type types
                    var tmpDb = new DbCollection<object>(dbConfig, lsDataTypes.ToArray());
                    var instance = DBConfig.GetInstance();
                    instance.AddDatabaseTypes(tmpDb.ToArray());
                    instance.AddDatabase(dbConfig);
                }
            }
        }

        public PersistanceSettings PersistanceSettings { get; private set; }

        private static Type GetEntityType(EntityConfiguration entity, EntitiesConfiguration entities)
        {
            string typeName;
            if (!entity.Absolute)
            {
                if (!String.IsNullOrEmpty(entities.Base))
                {
                    if (!string.IsNullOrEmpty(entities.Assembly)) typeName = $"{entities.Base}.{entity}, {entities.Assembly}";
                    else typeName = $"{entities.Base}.{entity}";
                }
                else if (!string.IsNullOrEmpty(entities.Assembly))
                {
                    typeName = $"{entity}, {entities.Assembly}";
                }
                else
                    typeName = entity.ToString();
            }
            else typeName = entity.Entity;
            return Type.GetType(typeName);
        }

        #region Databases
        public void AddType(params IDbListBase[] srcs)
        {
            AddDatabaseTypes(srcs);
        }

        public void AddDatabase(DatabaseConfiguration configuration)
        {
            Console.WriteLine("Added database: " + configuration.Value);
            Databases.Add(configuration);
        }


        /// <summary>
        /// Gets the general database that's used for all db operations.
        /// </summary>
        /// <returns></returns>
        public DatabaseConfiguration GetGeneralDatabase()
        {
            return GetDBByRole("general");
        }
        public DatabaseConfiguration GetDBByRole(string role)
        {
            return (from x in Databases where x.Role == role select x).FirstOrDefault();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcs"></param>
        public void AddDatabaseTypes(IDbListBase[] srcs)
        {
            for (int i = 0; i < srcs.Length; i++)
            {
                if (TypeBase == null) TypeBase = new Dictionary<Type, IDbListBase>();
                var src = srcs[i];
                var collType = src.GetType().GenericTypeArguments.FirstOrDefault();
                if (collType == null) continue;


                if (TypeBase.ContainsKey(collType))
                {
                    TypeBase[collType] = src;
                }
                else
                {
                    TypeBase.Add(collType, src);
                }
            }
        }

        public static void AddType<T>(IDbListBase src)
            where T : IEntityBase
        {
            if (TypeBase == null) TypeBase = new Dictionary<Type, IDbListBase>();
            TypeBase.Add(typeof(T), src);
        }

        #endregion 

        public static UserEntry GetUser(string type)
        {
            return Users.First(x => x.Type.ToLower().Equals(type.ToLower()));
        }


        public static IEntityRelation<TEntity> MapKeyRelation<TEntity, TValue>(Func<TValue, Expression<Func<TEntity, bool>>> predicate)
            where TEntity : class
        {
            IEntityRelation<TEntity> tEntityRelation = GetTypeRelation<TEntity>();
            tEntityRelation.And<TValue>(predicate);
            return tEntityRelation;
        }

        /// <summary>
        /// TODO: Resolve issues with expression compilation.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static IEntityRelation<TEntity> GetTypeRelation<TEntity>() where TEntity : class
        {
            var tEntity = typeof(TEntity);
            IEntityRelation<TEntity> tEntityRelation;
            if (mEntityRelations.ContainsKey(tEntity))
            {
                tEntityRelation = (IEntityRelation<TEntity>)mEntityRelations[tEntity];
            }
            else
            {
                tEntityRelation = new EntityRelation<TEntity>(tEntity);
            }
            mEntityRelations[tEntity] = tEntityRelation;
            return tEntityRelation;
        }

        public static DBConfig GetInstance(IConfiguration config = null)
        {
            if (_instance == null)
            {
                _instance = new DBConfig();
                Initialize(config);
            }
            return _instance;
        }
    }
}