using System.Collections.Generic;
using Donut.Data;
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces.Cloud;
using Netlyt.Interfaces.Models;
using Newtonsoft.Json.Linq;

namespace Donut
{
    public interface ILoggingService
    {
        void OnIntegrationViewed(ICloudNodeNotification nodeNotification, JToken body);
        void OnIntegrationCreated(ICloudNodeNotification nodeNotification, JToken body);
        IEnumerable<ActionLog> GetIntegrationLogs(DataIntegration ign);
    }
}