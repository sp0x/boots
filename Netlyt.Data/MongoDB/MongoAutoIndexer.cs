using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Driver;

namespace Netlyt.Data.MongoDB
{
    /// <summary>
    /// Automatic mongoDB type indexer. Uses attributes.
    /// </summary>
    /// <remarks></remarks>
    public class MongoAutoIndexer
    {

        #region "Static index builders"



        /// <summary>
        /// Builds a list of indexes which corespond to the tagged members of the given type.
        /// </summary>
        /// <typeparam name="TIndexType">The type to build the index from</typeparam>
        /// <param name="recursive">Should the builder evaluate recursive fields</param>
        /// <param name="ascending">Should the index be an ascending one</param>
        /// <returns>The list of indexes</returns>
        /// <remarks></remarks>
        public static IEnumerable<MongoIndex<TIndexType>> BuildIndexes<TIndexType>(bool recursive = false, bool @ascending = true) 
            where TIndexType  : class
        {
            return BuildIndexes< TIndexType>(typeof(TIndexType), recursive, @ascending);
        }
        private static List<MongoIndex<TRecord>> CreateIndex<TRecord>(Dictionary<UInt64, List<BoolString>> indexDict, bool @ascending)
        {
            var result = new List<MongoIndex<TRecord>>();
            foreach (var key in indexDict.Keys)
            { 
                string[] items = indexDict[key].Select((BoolString x) => x.Value).ToArray();
                var index = CreateFieldDefinitions<TRecord>(ascending, items);

                bool isUnique = Arrays.And(indexDict[key].Select(x => x.Bool));
                result.Add(new MongoIndex<TRecord>(index, unique: isUnique));
            }
            return result;
        }

        public static IndexKeysDefinition<TRecord> CreateFieldDefinitions<TRecord>(bool ascend, params string[] fields)
        {
            var keys = Builders<TRecord>.IndexKeys;
            var indexes = new List<IndexKeysDefinition<TRecord>>();
            foreach (var field in fields)
            {
                var index = ascend ? keys.Ascending(field) : keys.Descending(field);
                indexes.Add(index);
            }
            var kIndex = keys.Combine(indexes);
            return kIndex;

        }
        /// <summary>
        /// Builds a list of indexes which corespond to the tagged members of the given type.
        /// </summary>
        /// <param name="type">The type to build the index from</param>
        /// <param name="recursive">Should the builder evaluate recursive fields</param>
        /// <param name="ascending">Should the index be an ascending one</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static List<MongoIndex<T>> BuildIndexes<T>(Type type, bool recursive = false, bool @ascending = true, bool walkHierarchy = true)
        {
            Dictionary<UInt64, List<BoolString>> indexDict = null;
            Dictionary<Type, Dictionary<UInt64, List<BoolString>>> baseTypeMaps = new Dictionary<Type, Dictionary<UInt64, List<BoolString>>>();
            List<MongoIndex<T>> result = new List<MongoIndex<T>>();

            if (recursive)
            {
                //TODO: Implement filling of basetypemaps
                indexDict = GetRecursiveIndexedMembers(type, null, walkHierarchy);
            }
            else
            {
                indexDict = GetBaseIndexedMembers(type, walkHierarchy, ref baseTypeMaps);
            }
            if ((indexDict != null))
            {
                result = CreateIndex<T>(indexDict, @ascending);
                if (walkHierarchy && baseTypeMaps.Count > 0)
                {
                    foreach (Type typeMap in baseTypeMaps.Keys)
                    {
                        var tmpResult = CreateIndex<T>(baseTypeMaps[typeMap], @ascending);
                        result.AddRange(tmpResult);
                    }
                }
            }

            return result;
        }

        private static Dictionary<UInt64, List<BoolString>> GetBaseIndexedMembers(
            Type type,
            bool walkHierarchy = false,
            Dictionary<Type, Dictionary<UInt64, List<BoolString>>> hierarchyMapDicts = null)
        {
            return GetBaseIndexedMembers(type, walkHierarchy, ref hierarchyMapDicts);
        }

        /// <summary>
        /// Gets all mongo-indexed members of the type. Search is ran on base level.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static Dictionary<UInt64, List<BoolString>> GetBaseIndexedMembers(
            Type type, 
            bool walkHierarchy,
            ref Dictionary<Type, Dictionary<UInt64, List<BoolString>>> hierarchyMapDicts)
        {
            Type attribType = typeof(MongoIndexed);
            Type objAttribType = typeof (MongoIndexedObject);
            Dictionary<UInt64, List<BoolString>> outputDict = new Dictionary<UInt64, List<BoolString>>();
            Dictionary<UInt64, List<BoolString>> currentLevelOutput = new Dictionary<UInt64, List<BoolString>>();
            var baseLevel = false;
            hierarchyMapDicts = new Dictionary<Type, Dictionary<ulong, List<BoolString>>>();
            while (type.IsMappable())
            {
                //'Finds out if the type is indexed
                MongoIndexedObject objAttrib = (MongoIndexedObject)type.GetCustomAttribute(objAttribType);
                Boolean bUseInheritedProperites = objAttrib == null ? true : !objAttrib.ResetInheritedIndexes;
                type.GetCustomAttributes(attribType, true).ForEach(x =>
                {
                    dynamic attrib = x;
                    var firstAttrib = attrib.ConstructorArguments(0);
                    var ulVal = (ulong) firstAttrib.value;
                    if (baseLevel)
                    {
                        currentLevelOutput = currentLevelOutput.AddEx(ulVal, new BoolString(type.Name, firstAttrib.Unique));
                    }
                    else
                    {
                        outputDict = outputDict.AddEx(ulVal, new BoolString(type.Name, firstAttrib.Unique));
                    }
                    return null;
                });
                //'Finds out if the type has members who are indexed
                type.GetProperties()
                    .Where(x => x.GetCustomAttributes(attribType, true).Length > 0)
                    .ToList().ForEach(xprop =>
                    {
                        MongoIndexed mxAttrib = xprop.GetCustomAttribute<MongoIndexed>();
                        //Figure out which element was used for the index, prefer attribute element name, because it`s indexed
                        string elementName = mxAttrib.ElementName;
                        string propertyName = xprop.Name;
                        Boolean inherited = xprop.DeclaringType != null && xprop.DeclaringType != type;
                        if (string.IsNullOrEmpty(elementName))
                        {
                            elementName = propertyName;
                        }
                        //Filter inherited properties if needed!
                        if (!bUseInheritedProperites && inherited) return;

                        //0985
                        if (baseLevel)
                        {
                            currentLevelOutput.AddEx(mxAttrib.IndexGroup, new BoolString(elementName, mxAttrib.Unique));
                        }
                        else
                        {
                            outputDict.AddEx(mxAttrib.IndexGroup, new BoolString(elementName, mxAttrib.Unique));
                        }
                    });

                if (walkHierarchy)
                {
                    if (baseLevel)
                    {
                        hierarchyMapDicts.Add(type, currentLevelOutput.Clone());
                        currentLevelOutput.Clear();
                    }
                    baseLevel = true;
                    type = type.BaseType; 
                }
                else
                {
                    return outputDict;
                }
            }
            return outputDict;
        }

        private static Dictionary<UInt64, List<BoolString>> GetRecursiveIndexedMembers(Type rootType, string baseName = "", bool walkHierarchy = false)
        {
            Type attribType = typeof(MongoIndexed);
            Type objAttribType = typeof (MongoIndexedObject);
            Dictionary<UInt64, List<BoolString>> outputDict = new Dictionary<UInt64, List<BoolString>>();
            if (walkHierarchy)
            {
                throw new NotImplementedException();
            }
            MongoIndexedObject objIndex = (MongoIndexedObject)rootType.GetCustomAttribute(objAttribType, false);

            //'Finds out if the type is indexed
            rootType.GetCustomAttributes(attribType, true).ForEach(x =>
            {
                dynamic y = x;
                ulong ulVal = y.ConstructorArguments(0).value;
                return outputDict = outputDict.AddEx(ulVal, new BoolString(rootType.Name, false));
            });

            

            //'Finds out if the type has members who are MongoIndex`ed
            List<PropertyInfo> indexedProperties = rootType.GetProperties().Where(x =>
            {
                object[] objs = x.GetCustomAttributes(attribType, true);
                return objs!=null && objs.Length >0;
            }).ToList();

            foreach (var indexedProperty in indexedProperties)
            { 
                MongoIndexed attrib = indexedProperty.GetCustomAttribute<MongoIndexed>();
                if (attrib == null)
                    continue;
                if (attrib.Recursive)
                {
                    Dictionary<UInt64, List<BoolString>> rcDict = GetRecursiveIndexedMembers(indexedProperty.PropertyType, indexedProperty.Name);
                    outputDict.Append(rcDict);
                }
                else
                {
                    outputDict.AddEx(attrib.IndexGroup, new BoolString(compileMemberTree(baseName, indexedProperty.Name), attrib.Unique));
                }
                //' All recursive fields should contain the Recursive flag, in order for the index to be extended.
            }
            return outputDict;
        }
        private static string compileMemberTree(string @base, string node)
        {
            if (string.IsNullOrEmpty(@base))
                return node;
            return string.Format("{0}.{1}", @base, node);
        }


        
        #endregion
    }
}