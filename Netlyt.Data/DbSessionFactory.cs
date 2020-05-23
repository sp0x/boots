using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Netlyt.Data.SQL;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

namespace Netlyt.Data
{
    /// <summary>
    /// SqlDb SessionFactory setup, using NHibernate for ORM.
    /// Do not inherit this class
    /// </summary>
    /// <remarks></remarks>
    public class DbSessionFactory : IDisposable
    {
        /// <summary>
        /// A dictionary of session factories, indexed by the connection strings that were used for the session-s creation.
        /// </summary>

        private static Dictionary<string, ISessionFactory> _sessionFactories;


        public static bool ShowSQL = true;
        private string _MysqlConnectionStr = DBConfig.GetConnectionString();
        public Action<string> OnError { get; set; }


        public static DbSessionFactory Global;
        static DbSessionFactory()
        {
            Global = new DbSessionFactory { _MysqlConnectionStr = DBConfig.GetConnectionString() };
        }
        #region "Props"
        /// <summary>
        /// The EntryCollection to include in the mapping.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public static List<string> IncludedAssemblies { get; set; }
        #endregion


        #region "Session factory"
        /// <summary>
        /// 
        /// </summary>
        /// <param name="recreate"></param>
        /// <param name="mappingType"></param>
        /// <param name="underDebug"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        private static ISessionFactory FactoryCreate(bool recreate = false, 
                                                     MappingType mappingType = MappingType.NHibernate, 
                                                     bool underDebug = false,
                                                     DbSessionFactory factory = null)
        {
                if ((_sessionFactories == null))
                    _sessionFactories = new Dictionary<string, ISessionFactory>();

                ISessionFactory sesFact = null;
                if (factory != null)
                {
                    _sessionFactories.TryGetValue(factory._MysqlConnectionStr, out sesFact);
                }
                else
                {
                    //Use the default connection string that is always used
                    _sessionFactories.TryGetValue(DbSessionFactory.Global._MysqlConnectionStr, out sesFact);
                }

                if (sesFact == null)
                {
                    sesFact = Init(recreate, mappingType, underDebug, factory);
                }
                return sesFact;
            
        }
        private static DebugInterceptor Logger { get; set; }

        private static ISessionFactory Init(bool recreate = false, MappingType mapType = MappingType.FluentNHibernate, bool underDebug = false, DbSessionFactory factory = null)
        {
            // Modify your ConnectionString
            if (factory == null)
            {
                factory = Global;
            }
            try
            {
                MySQLConfiguration db = MySQLConfiguration.Standard.ConnectionString(factory._MysqlConnectionStr);
                if (underDebug)
                    db = MySQLConfiguration.Standard.ConnectionString(factory._MysqlConnectionStr).ShowSql(); 
                FluentConfiguration cfgx = Fluently.Configure().Database(db).Mappings((MappingConfiguration mc) =>
                {
                    AddMappings(mc, mapType);
                });

                if (recreate)
                    cfgx.ExposeConfiguration(cfgxx =>
                    {
                        SchemaExport sch = new SchemaExport(cfgxx);
                        sch.Create(true, true);
                    });
                if (underDebug)
                    cfgx.ExposeConfiguration(cfgxx => cfgxx.SetInterceptor(Logger));
                ISessionFactory fact = cfgx.BuildSessionFactory();
                _sessionFactories.Add(factory._MysqlConnectionStr, fact);
                return fact;
            }
            catch (Exception ex)
            {
                if (factory.OnError != null)
                {
                    ex.WalkException(factory.OnError, true);
                }
                else
                {
                    Extensions.ShowErrors(ex, true);
                }
            }
            return null;
        }

        private static MappingConfiguration AddMappings(MappingConfiguration m, MappingType type)
        {
            HashSet<AssemblyWrapper> localLibs = Extensions.GetProjectReferences(new PersistanceSettings());
            localLibs = localLibs.GetAssembliesToMap(IncludedAssemblies);
            foreach (AssemblyWrapper Assembly in localLibs)
            {
                //container.AddFromAssembly(Assembly.Assembly)
            }

            switch (type)
            {
                case MappingType.FluentNHibernate:
                    FluentMappingsContainer container = m.FluentMappings;
                    break;
                //                    container.AddFromAssemblyOf(Of SmapiRecord)()

                case MappingType.NHibernate:
                    //HbmMappingsContainer container = m.HbmMappings;
                    //break;
                //                    container.AddFromAssemblyOf(Of SmapiRecord)()

                case MappingType.AllSql:

                    FluentMappingsContainer fCont = m.FluentMappings;
                    HbmMappingsContainer hCont = m.HbmMappings;
                    break;
                    //                    fCont.AddFromAssemblyOf(Of SmapiRecord)()
                    //                    hCont.AddFromAssemblyOf(Of SmapiRecord)()

            }
            m.MergeMappings();

            return m;
        }
        #endregion

        #region "Opening"
        /// <summary>
        /// Proxy for the Create function!
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public static ISessionFactory Open(bool recreate = false, MappingType mappingType = MappingType.NHibernate, bool underDebug = false, string db = null, string endpoint = null, string user = null, string password = null, Action<string> onError = null)
        {
            //'Create the session factory if there's a custom db
            DbSessionFactory sfact = null;
            if ((db != null | endpoint != null))
            {
                sfact = new DbSessionFactory { _MysqlConnectionStr = DBConfig.GetConnectionString(db, endpoint, user, password) };
                sfact.OnError = onError;
            }
            return Create(recreate, mappingType, underDebug, sfact);
        }
        /// <summary>
        /// Proxy for the Create function!
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public static ISessionFactory Open(string db)
        {
            return Create(db);
        }
        #endregion

        #region "Creation"
        /// <summary>
        /// Create a session, using the alredy configured database
        /// </summary>
        /// <param name="recreate"></param>
        /// <param name="mappingType"></param>
        /// <param name="underDebug"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static ISessionFactory Create(bool recreate = false, MappingType mappingType = MappingType.NHibernate, bool underDebug = false, DbSessionFactory factory = null)
        {
            ISessionFactory fc = FactoryCreate(recreate, mappingType, underDebug, factory);
            return fc;
            //Return If(fc IsNot Nothing, fc.OpenSession(), Nothing)
        }
        /// <summary>
        /// Create a session with an existing database, instead of the configured one
        /// </summary>
        /// <param name="db"></param>
        /// <param name="recreate"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static ISessionFactory Create(string db, bool recreate = false, DbSessionFactory factory = null)
        {
            if (factory == null)
                factory = Global;
            if (!Extensions.HasVal(db))
                return Create(recreate, factory: factory);
            string conx = Regex.Replace(factory._MysqlConnectionStr, "(?<=database=).*?(;|$)", db, RegexOptions.IgnoreCase);
            // Modify your ConnectionString
            try
            {
                MySQLConfiguration dx = ShowSQL ? MySQLConfiguration.Standard.ConnectionString(conx).ShowSql() : MySQLConfiguration.Standard.ConnectionString(conx);
                return Fluently.Configure().Database(dx).Mappings(m => {
                    m.FluentMappings.AddFromAssembly(Assembly.GetExecutingAssembly());
                }).BuildSessionFactory();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);

                return null;
            }
        }
        #endregion



        public void Dispose()
        {
            if (_sessionFactories != null)
            {
                foreach(var sessionKeyVal in _sessionFactories)
                { 
                    ISessionFactory sf = sessionKeyVal.Value;
                    if ((sf == null))
                        continue;
                    if (!sf.IsClosed)
                    {
                        sf.Close();
                        sf.Dispose();
                    }                    
                }
                _sessionFactories.Clear();
            }
        }
    }
}