using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic; 
using System.Linq.Expressions;
using Dynamitey; 
using nvoid.db.DB; 
using nvoid.Integration;
using Netlyt.Service.Ml;
using Netlyt.Service.Source; 

namespace Netlyt.Service.Integration
{
    
    public partial class DataIntegration
        : Entity, IIntegration
    { 
        public long Id { get; set; }
        public virtual ICollection<ModelIntegration> Models { get; set; }
        public User Owner { get; set; }
        public string FeatureScript { get; set; }
        public string Name { get; set; }        
        public int DataEncoding { get; set; } 
        public virtual ApiAuth APIKey { get; set; } 
        public long? PublicKeyId { get; set; }
        public virtual ApiAuth PublicKey { get; set; }
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
        
        public ICollection<FieldDefinition> Fields { get; set; }
        public ICollection<IntegrationExtra> Extras { get; set; }

        public static DataIntegration Empty { get; set; } = new DataIntegration("Empty");

        public DataIntegration()
        { 
            Fields = new HashSet<FieldDefinition>();
            Models = new HashSet<ModelIntegration>();
            Extras = new HashSet<IntegrationExtra>();
        }
        public DataIntegration(string name)
            : this()
        { 
            this.Name = name;
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
            if (xpObj !=null)
            {
                var fields = xpObj as IDictionary<string, object>;
                foreach (var memberName in fields.Keys)
                {
                    var value = fields[memberName];
                    if (value == null) continue;
                    Type memberType = value.GetType();
                    var fieldDefinition = new FieldDefinition(memberName, memberType);
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
        public IntegratedDocument CreateDocument<T>(T data)
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
    }
}
