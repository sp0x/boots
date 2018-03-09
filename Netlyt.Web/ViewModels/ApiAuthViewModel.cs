using System.Collections.Generic;

namespace Netlyt.Web.ViewModels
{
    public class ApiAuthViewModel
    {
        public long Id { get;set; }
        public string AppId { get; set; }
    }

    public class FieldDefinitionViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

    }

    public class IntegrationExtraViewModel
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class DataIntegrationViewModel
    {
        public long Id { get; set; }
        public string FeatureScript { get; set; }
        public string Name { get; set; }
        public int DataEncoding { get; set; }
        public ApiAuthViewModel APIKey { get; set; }
        public string DataFormatType { get; set; }
        public string Source { get; set; }
        public ICollection<FieldDefinitionViewModel> Fields { get; set; }
        public ICollection<IntegrationExtraViewModel> Extras { get; set; }
    }
}