using Netlyt.Interfaces;

namespace Donut.Models
{
    public class ModelIntegration
    { 
        public long ModelId { get; set; }
        public Donut.Models.Model Model { get; set; } 
        public long IntegrationId { get; set; }
        public Data.DataIntegration Integration { get; set; }

        public ModelIntegration()
        {

        }
        public ModelIntegration(Donut.Models.Model model, Data.DataIntegration integration)
        {
            this.Model = model;
            this.Integration = integration;
            this.ModelId = model.Id;
            this.IntegrationId = integration.Id;
        }
    }
}