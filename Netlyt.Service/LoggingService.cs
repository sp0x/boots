using System;
using Donut;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service
{
    public class LoggingService : ILoggingService
    {
        public void OnIntegrationViewed(JToken body)
        {
            throw new NotImplementedException();
        }
    }
}