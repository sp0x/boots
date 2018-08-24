using Netlyt.Interfaces.Models;

namespace Netlyt.Service
{
    public interface IRateService
    {
        void SetAvailabilityForUser(string username, ApiRateLimit rate);
        void ApplyGlobal(ApiRateLimit quota);
        ApiRateLimit GetAllowed(User user);
        ApiRateLimit GetCurrentQuotaLeftForUser(User user);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        ApiRateLimit GetCurrentUsageForUser(User user);
        void ApplyDefaultForUser(User user);
    }
}