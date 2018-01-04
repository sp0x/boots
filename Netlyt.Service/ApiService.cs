using System.Linq;
using Microsoft.AspNetCore.Http;
using nvoid.Integration;
using Netlyt.Service.Data;

namespace Netlyt.Service
{
    public class ApiService
    {
        private IFactory<ManagementDbContext> _factory;
        private IHttpContextAccessor _contextAccessor;
        private ManagementDbContext _context;
        public ApiService(IFactory<ManagementDbContext> factory,
            IHttpContextAccessor contextAccessor,
            ManagementDbContext context)
        {
            _factory = factory;
            _context = context;
            _contextAccessor = contextAccessor;
        }

        public ApiAuth GetApi(long userApiId)
        {
            var api = _context.ApiKeys.Find(userApiId);
            return api;
        }
        public ApiAuth GetApi(string appId)
        {
            using (var context = _factory.Create())
            {
                var api = context.ApiKeys.FirstOrDefault(x => x.AppId == appId);
                return api;
            }
        }

        public ApiAuth Generate()
        {
            return ApiAuth.Generate();
        }

        public ApiAuth GetCurrentApi()
        {
            var appApiIdStr = _contextAccessor.HttpContext.Session.GetString("APP_API_ID");
            if (string.IsNullOrEmpty(appApiIdStr)) return null;
            var id = long.Parse(appApiIdStr);
            return GetApi(id);
        }
        public void SetCurrentApi(ApiAuth apiAuth)
        {
            var appApiId = _contextAccessor.HttpContext.Session.GetString("APP_API_ID");
            if (appApiId == null)
            {
                _contextAccessor.HttpContext.Session.SetString("APP_API_ID", apiAuth.Id.ToString());
            }
            _contextAccessor.HttpContext.Response.Headers.Add("APP_API_ID", apiAuth.Id.ToString());
        }
    }
}