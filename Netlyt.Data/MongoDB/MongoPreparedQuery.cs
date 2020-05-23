using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Netlyt.Data.MongoDB
{
 /// <summary>
    /// 
    /// 
    /// 
    /// 
    /// Holds query objects, and builds a query upon request, from a generic object, fitting the query parameters.
    /// </summary>
    /// <remarks></remarks>
    public class MongoPreparedQuery : BsonDocument
    {
        private MongoPreparedQuery(FilterDefinition<BsonDocument> qrDoc, bool trim = false) :
            this(qrDoc.ToBsonDocument().Clone().AsBsonDocument, trim)
        {
        }
        private MongoPreparedQuery(BsonDocument qrDoc, bool trim = false) : base()
        {
            foreach (var element in qrDoc.Elements)
            {
                string nameSubstr = element.Name;//.Substring(0, elem.Name.IndexOf(".", System.StringComparison.Ordinal) + 1);
                string name = nameSubstr;//trim ? elem.Name.Replace(nameSubstr, "") : "";
                base.Set(name, BsonNull.Value);
            }
        }
        /// <summary>
        /// Creates a new prepared mongodb query
        /// </summary>
        /// <param name="qrDoc"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static MongoPreparedQuery Create(FilterDefinition<BsonDocument> qrDoc)
        {
            return new MongoPreparedQuery(qrDoc);
        }
        /// <summary>
        /// Creates a new prepared mongodb query
        /// </summary>
        /// <param name="qrDoc"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static MongoPreparedQuery Create<TRecord>(FilterDefinition<TRecord> qrDoc)
        {
            return new MongoPreparedQuery(qrDoc.ToBsonDocument());
        }
        public static MongoPreparedQuery CreateTrimmed(FilterDefinition<BsonDocument> qrDoc)
        {
            return new MongoPreparedQuery(qrDoc, true);
        }
        /// <summary>
        /// Builds the MongoDb IMongoQuery object, based on the existing query parameters specified on construction.
        /// </summary>
        /// <param name="value">The values to use as query arguments</param>
        /// <param name="parentElementName">The name of the value field, from it's parent object.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public BsonDocument Build(object value, string parentElementName = null)
        {
            Type targetType = value.GetType();
            List<BsonElement> cpElements = new List<BsonElement>();
            string elementName = null;
            for (Int32 i = 0; i < Elements.Count(); i++)
            {
                BsonValue qrElementValue = BsonNull.Value;
                elementName = Elements.ElementAt(i).Name;
                if (!String.IsNullOrEmpty(parentElementName))
                {
                    elementName = elementName.Replace(string.Format("{0}.", parentElementName), "");
                }
                else
                {
                    elementName = elementName.Substring(Elements.ElementAt(i).Name.IndexOf(".") + 1);
                }
                PropertyInfo prop = targetType.GetProperty(elementName);
                if (prop == null)
                {
                    prop = MongoIndexed.GetMemberByElementKey(value.GetType(), elementName);
                    if(prop==null) continue;
                    //cpElements.RemoveAt(i) ' Element is empty 
                }

                if(prop!=null)
                {
                    if (!Extensions.TryCreateBsonValue(prop.GetValue(value), ref qrElementValue))
                    {
                        Trace.WriteLine("Mongo prepared query builder failed to build query!");
                        //Something went wrong
                    }
                    if (!string.IsNullOrEmpty(parentElementName))
                    {
                        elementName = string.Format("{0}.{1}", parentElementName, elementName);
                    }
                }
                cpElements.Add(new BsonElement(elementName, qrElementValue));
            }
            var doc = new BsonDocument(cpElements.Cast<BsonElement>());
            return doc;
        }

    }
}