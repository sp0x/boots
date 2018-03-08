using Netlyt.Service.Integration;

namespace Netlyt.Service.Ml
{
    public class ModelIntegration
    { 
        public long ModelId { get; set; }
        public Model Model { get; set; }

        public long IntegrationId { get; set; }
        public DataIntegration Integration { get; set; }

        public ModelIntegration(Model model, DataIntegration integration)
        {
            this.Model = model;
            this.Integration = integration;
            this.ModelId = model.Id;
            this.IntegrationId = integration.Id;
        }
    }
}