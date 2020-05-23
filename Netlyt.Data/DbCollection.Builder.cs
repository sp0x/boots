using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Netlyt.Data.MongoDB;
using Netlyt.Data.SQL;

namespace Netlyt.Data
{
  public partial class DbCollection<TRecord> : List<IDbListBase> where TRecord : class, new()
    {
        public class Builder
        {
            private Type _collectionType;
            private string _url;
            private string _name;

            public Builder(string name)
            { this._name = name; }

            public Builder SetCollectionType(Type typ) { this._collectionType = typ; return this; }

            public Builder SetCollectionType(DatabaseType typ)
            {
                switch (typ)
                {
                    case DatabaseType.MongoDb: _collectionType = typeof(MongoList<>); break;
                    case DatabaseType.MySql: _collectionType = typeof(SQLList<>); break;
                }
                return this;
            }
            public Builder SetUrl(string url) { this._url = url; return this; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="dataType"></param>
            /// <returns></returns>
            public IDbListBase GetCollection(Type dataType)
            {
                var databaseCollection = _collectionType.MakeGenericType(dataType);
                var typeName = dataType.Name;
                return (IDbListBase)Extensions.Ctor(databaseCollection, _name, typeName, _url);
            }

            public IEnumerable<IDbListBase> GetCollections(Type[] types)
            {
                for (var itype = 0; itype <= types.Length - 1; itype++)
                {
                    if (types[itype] == null) continue;
                    var type = types[itype];
                    IDbListBase database = null;
                    try
                    {
                        database = GetCollection(type);
                    }
                    catch (Exception ex)
                    {
                        var targetException = ex;
                        if(ex is TargetInvocationException)
                        {
                            targetException = ex.InnerException;
                        }
                        
                        var exceptionType = targetException.GetType().Name;
                        var formattedError = $"Could not create a {this._collectionType.ToString()} list of type {type.Name}!\n" +
                                             $"Connection string: {this._url}\n" +
                                             $"Error[{exceptionType}]: {targetException.Message}";
                        if (targetException is ReflectionTypeLoadException)
                        {
                            var loaderException = (targetException as ReflectionTypeLoadException).LoaderExceptions;
                            var exceptionMessages = String.Join(Environment.NewLine, loaderException.Select(x => $"{x.GetType().Name}: {x.Message}").ToArray());
                            formattedError += $"\n{exceptionMessages}";
                            
                        }
                        
                        Trace.WriteLine(formattedError);
                        Debug.WriteLine(formattedError);
                        Console.WriteLine(formattedError);
                        continue;
                    }
                    if (database == null) continue;
                    yield return database;
                }
            }
        }
    }
}