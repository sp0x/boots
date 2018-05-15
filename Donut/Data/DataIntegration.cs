using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Donut.Integration;
using Donut.Models;
using Donut.Source;
using Dynamitey;
using Netlyt.Interfaces;
using Netlyt.Service.Integration;

//using Netlyt.Service.Ml; 

namespace Donut.Data
{
    public partial class DataIntegration : IIntegration
    {
        //private List<AggregateKey> _aggregateKeys;
        public long Id { get; set; }
        public virtual ICollection<ModelIntegration> Models { get; set; }
        public User Owner { get; set; }
        public string FeatureScript { get; set; }
        public string Name { get; set; }
        public int DataEncoding { get; set; }
        [ForeignKey("APIKey")]
        public long APIKeyId { get; set; }
        public ApiAuth APIKey { get; set; }
        public long? PublicKeyId { get; set; }
        public virtual ApiAuth PublicKey { get; set; }
        public virtual ICollection<AggregateKey> AggregateKeys { get; set; }
        /// <summary>
        /// the type of the data e.g stream or file
        /// </summary>
        public string DataFormatType { get; set; }
        /// <summary>
        /// The source from which the integration is registered to receive data.
        /// Could be url or just a hint.
        /// </summary>
        public string Source { get; set; }
        public string Collection { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DataIndexColumn { get; set; }
        /// <summary>
        /// Name of the data's timestamp column
        /// </summary>
        public string DataTimestampColumn { get; set; }
        public string FeaturesCollection { get; set; }

        public ICollection<FieldDefinition> Fields { get; set; }
        public ICollection<IntegrationExtra> Extras { get; set; }

        public static DataIntegration Empty { get; set; } = new DataIntegration("Empty");


        public DataIntegration()
        {
            Fields = new HashSet<FieldDefinition>(new FieldDefinitionComparer());
            Models = new HashSet<ModelIntegration>();
            Extras = new HashSet<IntegrationExtra>();
            AggregateKeys = new HashSet<AggregateKey>();
            this.PublicKey = ApiAuth.Generate();
        }
        public DataIntegration(string name, bool generateCollections = false)
            : this()
        {
            this.Name = name;
            if (generateCollections)
            {
                Collection = Guid.NewGuid().ToString();
                FeaturesCollection = $"{Collection}_features";
            }
        }

        /// <summary>
        /// Resolves the fields from a given instance object, using it's type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public DataIntegration SetFieldsFromType<T>(T instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            Fields = new List<FieldDefinition>();
            var type = typeof(T);
            ExpandoObject xpObj = instance as ExpandoObject;
            var dateParser = new DateParser();
            if (xpObj != null)
            {
                var fields = xpObj as IDictionary<string, object>;
                foreach (var memberName in fields.Keys)
                {
                    var value = fields[memberName];
                    if (value == null) continue;
                    DateTime timeValue;
                    double? doubleValue;
                    //The formatter is responsible for parsing the type, we don`t care about it..
                    //var isDateTime = dateParser.TryParse(value.ToString(), out timeValue, out doubleValue);
                    //if (doubleValue != null) value = doubleValue;
                    //else if (isDateTime) value = timeValue;
                    Type memberType = value.GetType();
                    var fieldDefinition = new FieldDefinition(memberName, memberType);
                    //TODO: move this to a factory method
                    if (value is string)
                    {
                        fieldDefinition.DataEncoding = FieldDataEncoding.BinaryIntId;
                        fieldDefinition.Extras = new FieldExtras();
                        //fieldDefinition.Extras.Field = fieldDefinition;
                    }
                    Fields.Add(fieldDefinition);
                }
            }
            else
            {
                var tInstance = instance as IDynamicMetaObjectProvider;
                if (tInstance != null)
                {
                    var dynamicMetaObject = tInstance.GetMetaObject(Expression.Constant(tInstance));
                    var dynMembers = dynamicMetaObject.GetDynamicMemberNames();
                    foreach (var memberName in dynMembers)
                    {
                        dynamic memberValue = Dynamic.InvokeGet(instance, memberName);
                        if (memberValue == null) continue;
                        Type memberType = memberValue.GetType();
                        var fieldDefinition = new FieldDefinition(memberName, memberType);
                        Fields.Add(fieldDefinition); //memberName
                    }
                }
                else
                {
                    var props = type.GetProperties();
                    if (props != null)
                    {
                        foreach (var property in props)
                        {
                            dynamic memberValue = property.GetValue(instance);
                            if (memberValue == null) continue;
                            Type memberType = memberValue.GetType();
                            var fieldDefinition = new FieldDefinition(property.Name, memberType);
                            Fields.Add(fieldDefinition); //property.Name
                        }
                    }
                }
            }
            return this;
        }

        public string GetReducedCollectionName()
        {
            return $"{Collection}_reduced";
        }
        public IIntegratedDocument CreateDocument<T>(T data)
        {
            var doc = new IntegratedDocument();
            doc.SetDocument(data);
            doc.IntegrationId = Id;
            doc.APIId = this.APIKey.Id;
            return doc;
        }

        public void AddField(string fieldName, Type type)
        {
            var fdef = new FieldDefinition(fieldName, type);
            Fields.Add(fdef); //fieldName
        }
        public FieldDefinition AddField<TField>(string fieldName, FieldDataEncoding encoding = FieldDataEncoding.None)
        {
            var fdef = new FieldDefinition(fieldName, typeof(TField));
            fdef.DataEncoding = encoding;
            (Fields as HashSet<FieldDefinition>)?.Add(fdef); //fieldName
            return fdef;
        }

//        public void SetAggregatekeys(IEnumerable<AggregateKey> keys)
//        {
//            _aggregateKeys = keys.ToList();
//        }


        public static DataIntegration Wrap(IIntegration ign)
        {
            return ign as DataIntegration;
        }
    }

    public class FieldDefinitionComparer : IEqualityComparer<IFieldDefinition>
    {
        public bool Equals(IFieldDefinition x, IFieldDefinition y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(IFieldDefinition obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
