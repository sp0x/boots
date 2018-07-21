using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Netlyt.Web.ViewModels
{
    public class FieldDefinitionViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string DType { get; set; }
        public string Type { get; set; }
        public string TargetType { get; set; }
        public JObject Description { get; set; }

    }
}