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

}