using System.Collections.Generic;
using System.Security.Claims;
using Donut.Integration;
using Donut.Models;
using Netlyt.Interfaces.Models;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Cloud
{
    public interface INotificationService
    {
        void SendRegisteredNotification(User resultItem2);
        void SendLoggedInNotification(User httpContextUser);
        void SendNewIntegrationSummary(IIntegration newIntegration);
        void SendIntegrationViewed(long viewedIntegrationId);
        void SendModelCreated(Model newModel);
        void SendModelBuilding(Model model, JToken trainingTask);
        void SendModelTrained(Model model, List<ModelTrainingPerformance> targetPerformances);
    }
}