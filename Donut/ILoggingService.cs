using Newtonsoft.Json.Linq;

namespace Donut
{
    public interface ILoggingService
    {
        void OnIntegrationViewed(JToken body);
    }
}