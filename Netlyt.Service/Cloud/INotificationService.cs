using System.Security.Claims;
using Donut.Integration;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Cloud
{
    public interface INotificationService
    {
        void SendRegisteredNotification(User resultItem2);
        void SendLoggedInNotification(ClaimsPrincipal httpContextUser);
        void SendNewIntegrationSummary(IIntegration newIntegration);
    }
}