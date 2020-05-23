using System;
using System.Reflection;

namespace Netlyt.Data.MongoDB
{
      [AttributeUsage(AttributeTargets.Property)]
    public class MongoIndexed : Attribute
    {
        /// <summary>
        /// Used to group indexes
        /// </summary>
        public UInt64 IndexGroup { get; set; }
        public bool OverrideMember { get; set; }
        /// <summary>
        /// Wether the field/property contains any members which are indexed. Extend the index to these fields.
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool Recursive { get; set; }
        public bool Unique { get; set; }
        public string ElementName { get; set; }
        public MongoIndexed(UInt64 category)
        {
            this.IndexGroup = category;
        }
         

        /// <summary>
        /// Get the property info of an element by it's element name.
        /// </summary>
        /// <param name="elementName"></param>
        /// <returns></returns>
        public static PropertyInfo GetMemberByElementKey(Type type, String elementName)
        {
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                MongoIndexed mxAttrib = property.GetCustomAttribute<MongoIndexed>();
                if (mxAttrib == null) continue;
                if (!string.IsNullOrEmpty(mxAttrib.ElementName) && mxAttrib.ElementName.Equals(elementName))
                {
                    return property;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the property info of an element by it's element name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elementName"></param>
        /// <returns></returns>
        public static PropertyInfo GetMemberByElementKey<T>(String elementName)
        {
            return GetMemberByElementKey(typeof (T), elementName);
        }
    }

    /// <summary>
    /// Gives access to inheritance control, for serialization options.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MongoIndexedObject : Attribute
    {
        /// <summary>
        /// Use inherited indexes?
        /// </summary>
        public Boolean ResetInheritedIndexes { get; set; } 
        
        public MongoIndexedObject(Boolean resetInheritedIndexes)
        {
            this.ResetInheritedIndexes = resetInheritedIndexes;
        }


        /// <summary>
        /// Get the property info of an element by it's element name.
        /// </summary>
        /// <param name="elementName"></param>
        /// <returns></returns>
        public static PropertyInfo GetMemberByElementKey(Type type, String elementName)
        {
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                MongoIndexed mxAttrib = property.GetCustomAttribute<MongoIndexed>();
                if (mxAttrib == null) continue;
                if (!string.IsNullOrEmpty(mxAttrib.ElementName) && mxAttrib.ElementName.Equals(elementName))
                {
                    return property;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the property info of an element by it's element name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elementName"></param>
        /// <returns></returns>
        public static PropertyInfo GetMemberByElementKey<T>(String elementName)
        {
            return GetMemberByElementKey(typeof(T), elementName);
        }
    }
}