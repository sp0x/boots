using Newtonsoft.Json.Linq;

namespace Netlyt.Data.ViewModels
{
    public class FieldDefinitionViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string DType { get; set; }
        public string Type { get; set; }
        public string TargetType { get; set; }
        public JObject Description { get; set; }
        public string Language { get; set; }

    }
}