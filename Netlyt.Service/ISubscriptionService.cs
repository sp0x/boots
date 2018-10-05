using Netlyt.Interfaces.Models;

namespace Netlyt.Service
{
    public interface ISubscriptionService
    {
        Token SubscribeForAccess(string email, string forService = "Netlyt", bool sendNotification = false);
    }
}