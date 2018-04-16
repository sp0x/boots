using System.Collections.Generic;
using Netlyt.Web.ViewModels;

namespace Netlyt.Web.Controllers
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