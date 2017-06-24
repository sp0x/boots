using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using Dynamitey;
using nvoid.db.DB;
using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;

namespace Peeralize.Service.Integration
{
    /// <summary>
    /// Definition of a type which is used in an integeration.
    /// </summary>
    public class IntegrationTypeDefinition : Entity, IIntegrationTypeDefinition
    {
        public string Name { get; set; }
        public int CodePage { get; set; }
        public string OriginType { get; set; } 
        public string UserId { get; set; }
        public Dictionary<string, FieldDefinition> Fields { get; set; }
        public IntegrationTypeExtras Extras { get; set; }

        public IntegrationTypeDefinition() { }
        public IntegrationTypeDefinition(string name) : this()
        {
            this.Name = name; 
        }

        /// <summary>
        /// Gets the integration data type from this source
        /// </summary>
        /// <param name="fileSrc"></param>
        /// <returns></returns>
        public static IntegrationTypeDefinition CreateFromSource(IInputSource fileSrc)
        { 
            var structure = fileSrc.GetTypeDefinition();
            return structure as IntegrationTypeDefinition;
        }

        /// <summary>
        /// Resolves the fields from a given instance object, using it's type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public IntegrationTypeDefinition ResolveFields<T>(T instance)
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

        public override void PrepareForSaving()
        {
            base.PrepareForSaving();
            if (string.IsNullOrEmpty(UserId))
                throw new InvalidOperationException("Only user owned type definitions can be saved!");
        }
         
    }
}