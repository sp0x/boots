using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Netlyt.Data.ViewModels
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

        public void AddDataDescription(JToken description)
        {
            var summary = description["file_summary"];
            var scheme = summary["scheme"];
            var descs = summary["desc"];
            var fields = Fields.ToList();
            foreach (JProperty fieldPair in scheme)
            {
                var fname = fieldPair.Name;
                var fld = fields.FirstOrDefault(x => x.Name == fname);
                if (fld == null) continue;
                fld.DType = fieldPair.Value.ToString();
            }
            foreach (JProperty descPair in descs)
            {
                var fname = descPair.Name;
                var fld = fields.FirstOrDefault(x => x.Name == fname);
                if (fld == null) continue;
                fld.Description = descPair.Value as JObject;
            }
            this.Fields = fields;
        }
    }
    
    public class ModelTargetViewModel
    {
        public long Id { get; set; }
        public long ModelId { get; set; }
        public FieldDefinitionViewModel Column { get; set; }
    }
}