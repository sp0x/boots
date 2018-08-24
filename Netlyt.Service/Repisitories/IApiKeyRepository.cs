using System.Linq;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Repisitories
{
    public interface IApiKeyRepository
    {
        IQueryable<ApiUser> GetForUser(User id);
        ApiAuth GetById(long apiKeyId);
    }
}