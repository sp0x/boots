namespace Netlyt.Data.ViewModels
{
    public class CreateAutomaticModelViewModel
    {
        public long IntegrationId { get; set; }
        public string Name { get; set; }
        public ShortFieldDefinitionViewModel Target { get; set; }
        public ShortFieldDefinitionViewModel IdColumn { get; set; }
        public string UserEmail { get; set; }
        public bool GenerateFeatures { get; set; }
    }

    public class DonutScriptUpdateViewModel
    {
        public string Donut { get; set; }
        public string Python { get; set; }
    }
}