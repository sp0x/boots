namespace Netlyt.Web.ViewModels
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
}