using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Dynamitey; 
using nvoid.db.DB;
using nvoid.db.Extensions;
using nvoid.Integration;
using Netlyt.Service.Ml;
using Netlyt.Service.Source;
using NHibernate.Util;

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
        public ApiAuth APIKey { get; set; }
        public string DataFormatType { get; set; }
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
            if (instance is ExpandoObject)
            {
                var fields = instance as IDictionary<string, object>;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="appId"></param>
        /// <param name="existingDefinition"></param>
        /// <returns></returns>
        public static bool Exists(IIntegration type, string appId, out DataIntegration existingDefinition)
        {
            var _typeStore = typeof(DataIntegration).GetDataSource<DataIntegration>();
            var integration = _typeStore.Where(x => x.APIKey.AppId == appId && (x.Fields == type.Fields || x.Name == type.Name));
            if (integration == null || integration.Count() == 0)
            {
                existingDefinition = null;
                return false;
            }
            existingDefinition = integration.First();
            return existingDefinition != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="apiId"></param>
        /// <param name="existingDefinition"></param>
        /// <returns></returns>
        public static bool Exists(IIntegration type, long apiId, out DataIntegration existingDefinition)
        {
            var _typeStore = typeof(DataIntegration).GetDataSource<DataIntegration>();
            var integration = _typeStore.Where(x => x.APIKey.Id == apiId && (x.Fields == type.Fields || x.Name == type.Name));
            if (integration == null || integration.Count() == 0)
            {
                existingDefinition = null;
                return false;
            }
            existingDefinition = integration.First();
            return existingDefinition != null;
        }
        //
        //        public override void PrepareForSaving()
        //        {
        //            base.PrepareForSaving();
        //            if (string.IsNullOrEmpty(APIKey))
        //                throw new InvalidOperationException("Only user owned type definitions can be saved!");
        //        }

        /// <summary>
        /// Checks if this type already exists, and updates it's id if it does.
        /// If not, the type is saved.
        /// </summary>
        /// <param name="userApiId">The user's API id, to which to subscribe the type</param>
        /// <returns></returns>
        public IIntegration SaveType(string userApiId)
        {
            DataIntegration oldTypeDef = null;
            if (!Exists(this, userApiId, out oldTypeDef))
            {
                this.Save();
            }
            else
            {
                this.Id = oldTypeDef.Id;
            }
            return this as IIntegration;
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
