using System.Linq;
using nvoid.Integration;
using Netlyt.Service.Data;

namespace Netlyt.Service
{
    public class ApiService
    {
        private IFactory<ManagementDbContext> _factory;

        public ApiService(IFactory<ManagementDbContext> factory)
        {
            _factory = factory;
        }

        public ApiAuth GetApi(long userApiId)
        {
            using (var context = _factory.Create())
            {
                var api = context.ApiKeys.FirstOrDefault(x => x.Id == userApiId);
                return api;
            }
        }
        public ApiAuth GetApi(string appId)
        {
            using (var context = _factory.Create())
            {
                var api = context.ApiKeys.FirstOrDefault(x => x.AppId == appId);
                return api;
            }
        }
    }
}