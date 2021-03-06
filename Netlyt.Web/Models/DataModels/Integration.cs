﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Dynamitey;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;
using nvoid.db.DB;
using nvoid.db.Extensions;
using Netlyt.Service.IntegrationSource;

namespace Netlyt.Web.Models.DataModels
{
    
    public partial class Integration
        : Entity, IIntegration
    { 
        public long Id { get; set; }
        public List<Model> Models { get; set; }
        public User Owner { get; set; }
        public string FeatureScript { get; set; }
        public string Name { get; set; }        
        public int DataEncoding { get; set; }
        public string APIKey { get; set; }
        public string DataFormatType { get; set; }
        public string OriginType { get; set; } // the type of the data e.g stream or file
        public string Collection { get; set; }
        public Dictionary<string, FieldDefinition> Fields { get; set; }
        public IntegrationTypeExtras Extras { get; set; }
        public static IntegrationTypeDefinition Empty { get; set; } = new IntegrationTypeDefinition("Empty");

        public Integration(string name)
        {
            Fields = new Dictionary<string, FieldDefinition>();
            Extras = new IntegrationTypeExtras();
            this.Name = name;
        }

        /// <summary>
        /// Resolves the fields from a given instance object, using it's type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public Integration SetFieldsFromType<T>(T instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            Fields = new Dictionary<string, FieldDefinition>();
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
                    Fields.Add(memberName, fieldDefinition);
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
                        Fields.Add(memberName, fieldDefinition);
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
                            Fields.Add(property.Name, fieldDefinition);
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
        /// <param name="apiId"></param>
        /// <param name="existingDefinition"></param>
        /// <returns></returns>
        public static bool Exists(Integration type, string apiId, out Integration existingDefinition)
        {
            var _typeStore = typeof(Integration).GetDataSource<Integration>();
            var integration = _typeStore.Where(x => x.APIKey == apiId && (x.Fields == type.Fields || x.Name == type.Name));
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
            Integration oldTypeDef = null;
            if (!Exists(this, userApiId, out oldTypeDef))
            {
                this.Save();
            }
            else
            {
                this.Id = oldTypeDef.Id;
            }
            return this;
        }

        public IntegratedDocument CreateDocument<T>(T data)
        {
            var doc = new IntegratedDocument();
            doc.SetDocument(data);
            doc.IntegrationId = Id;
            doc.APIKey = this.APIKey;
            return doc;
        }

        public void AddField(string fieldName, Type type)
        {
            var fdef = new FieldDefinition(fieldName, type);
            Fields.Add(fieldName, fdef);
        }
    }
}
