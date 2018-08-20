using System.Collections.Generic;

namespace Netlyt.Data.ViewModels
{
    public class CreateEmptyModelViewModel
    {
        public long IntegrationId { get; set; }
        public string ModelName { get; set; }
        public bool GenerateFeatures { get; set; }
        public string CallbackUrl { get; set; }
        public ShortFieldDefinitionViewModel IdColumn { get; set; }
        public IEnumerable<TargetSelectionViewModel> Targets { get; set; }
        public IEnumerable<string> FeatureCols { get; set; }
    }
}