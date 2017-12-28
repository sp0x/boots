using System.Collections.Generic;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;
using nvoid.db.DB;

namespace Netlyt.Web.Models.DataModels
{
    public class Integration
        : Entity
    {
        public long Id { get; set; }
        public List<Model> Models { get; set; }
        public User Owner { get; set; }
        public string FeatureScript { get; set; }
        public string Name { get; set; }        
        public int DataEncoding { get; set; }
        public string APIKey { get; set; }
        public string DataFormatType { get; set; }
        public string Collection { get; set; }
        public Dictionary<string, FieldDefinition> Fields { get; set; }
        public IntegrationTypeExtras Extras { get; set; }
        public static IntegrationTypeDefinition Empty { get; set; } = new IntegrationTypeDefinition("Empty");
    }
}
