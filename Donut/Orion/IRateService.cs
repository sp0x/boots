using Netlyt.Interfaces.Models;

namespace Donut.Orion
{
    public interface IRateService
    {
        ApiRateLimit GetAllowed(User user);
    }
}