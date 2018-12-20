using System.Threading.Tasks;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service
{
    public interface ISubscriptionService
    {
        Task<Token> SubscribeForAccess(string email, string forService = "Netlyt", bool sendNotification = false);
    }
}