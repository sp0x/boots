using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Integration;
using Netlyt.Service.IntegrationSource;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration
{
    public partial class DataIntegration
    {
        public class Factory
        {
            /// <summary>
            /// Gets the integration data type from this source
            /// </summary>
            /// <param name="fileSrc"></param>
            /// <returns></returns>
            public static Service.Integration.DataIntegration CreateFromSource(IInputSource fileSrc)
            {
                var structure = fileSrc.GetTypeDefinition();
                return structure as Service.Integration.DataIntegration;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public static Service.Integration.DataIntegration CreateFromType<T>(string name, string appId)
            {
                var type = typeof(T);
                var typedef = new Service.Integration.DataIntegration(type.Name);
                typedef.APIKey = appId;
                typedef.DataFormatType = "dynamic";
                typedef.DataEncoding = System.Text.Encoding.Default.CodePage;
                var properties = type.GetProperties();
                //var fields = type.GetFields(); 
                foreach (var member in properties)
                {
                    Type memberType = member.PropertyType;
                    var fieldDefinition = new FieldDefinition(member.Name, memberType);
                    typedef.Fields.Add(member.Name, fieldDefinition);
                }
                typedef.Name = name;
                return typedef;
            }
            public static Service.Integration.DataIntegration CreateNamed(string appId, string name)
            {
                var typedef = new Service.Integration.DataIntegration(name);
                typedef.APIKey = appId;
                return typedef;
            }
        }
    }
}
