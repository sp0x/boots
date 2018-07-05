using System.Collections.Generic;

namespace Netlyt.Web.ViewModels
{
    public class IntegrationSchemaViewModel
    {
        public IEnumerable<FieldDefinitionViewModel> Fields { get; set; }
        public long IntegrationId { get; set; }
        public IntegrationSchemaViewModel(long ignId, IEnumerable<FieldDefinitionViewModel> schema)
        {
            this.Fields = schema;
            this.IntegrationId = ignId;
        }
    }
}