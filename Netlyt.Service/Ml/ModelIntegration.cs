using Netlyt.Service.Integration;

namespace Netlyt.Service.Ml
{
    public class ModelIntegration
    { 
        public long ModelId { get; set; }
        public Model Model { get; set; }

        public string IntegrationId { get; set; }
        public DataIntegration Integration { get; set; }
    }
}