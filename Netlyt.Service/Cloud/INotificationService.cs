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
        void SendNewIntegrationSummary(IIntegration newIntegration, User user);
        void SendIntegrationViewed(long viewedIntegrationId, string userId);
        void SendModelCreated(Model newModel, User user);
        void SendModelBuilding(Model model, User user, JToken trainingTask);
        void SendModelTrained(Model model, User user, List<ModelTrainingPerformance> targetPerformances);
    }
}