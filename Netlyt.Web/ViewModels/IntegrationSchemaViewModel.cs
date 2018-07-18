using System.Collections.Generic;

namespace Netlyt.Web.ViewModels
{
    public class IntegrationSchemaViewModel
    {
        public IEnumerable<FieldDefinitionViewModel> Fields { get; set; }
        public IEnumerable<ModelTargetViewModel> Targets { get; set; }
        public long IntegrationId { get; set; }
        public IntegrationSchemaViewModel(long ignId, IEnumerable<FieldDefinitionViewModel> fields)
        {
            this.Fields = fields;
            this.IntegrationId = ignId;
        }
    }
    
    public class ModelTargetViewModel
    {
        public long Id { get; set; }
        public long ModelId { get; set; }
        public FieldDefinitionViewModel Column { get; set; }
    }
}