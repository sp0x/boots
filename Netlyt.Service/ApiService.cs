using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using nvoid.Integration;
using Netlyt.Service.Data;

namespace Netlyt.Service
{
    public class ApiService
    { 
        private IHttpContextAccessor _contextAccessor;
        private ManagementDbContext _context;
        public ApiService(ManagementDbContext context,
            IHttpContextAccessor contextAccessor)
        { 
            _context = context;
            _contextAccessor = contextAccessor;
        }

        public ApiAuth GetApi(long userApiId)
        {
            //using (var context = _factory.Create())
            //{
            var api = _context.ApiKeys.Find(userApiId);
            return api;
            //}
        }
        public ApiAuth GetApi(string appId)
        {
            //            using (var context = _factory.Create())
            //            {
            var api = _context.ApiKeys.FirstOrDefault(x => x.AppId == appId);
            return api;
            //}
        }

        /// <summary>
        /// Generates a new api auth key.
        /// </summary>
        /// <returns></returns>
        public ApiAuth Generate()
        {
            return ApiAuth.Generate();
        }

        public User GetCurrentApiUser()
        {
            var appApiIdStr = _contextAccessor.HttpContext.Session.GetString("APP_API_ID");
            if (string.IsNullOrEmpty(appApiIdStr)) return null;
            var id = long.Parse(appApiIdStr);
            var user = _context.Users.FirstOrDefault(x => x.ApiKeys.Any(a => a.Id == id));
            return user;
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

        public User GetApiUser(ApiAuth api)
        {
            var user = _context.Users.FirstOrDefault(x => x.ApiKeys.Any(a=> a.Id == api.Id));
            return user;
        }

        public void Register(ApiAuth key)
        {
            _context.ApiKeys.Add(key);
            _context.SaveChanges();
        }
        public async Task RegisterAsync(ApiAuth key)
        {
            _context.ApiKeys.Add(key);
            await _context.SaveChangesAsync();
        }

        public void RemoveKey(ApiAuth appId)
        {
            _context.ApiKeys.Remove(appId);
            _context.SaveChanges();
        }

        public IQueryable<ApiAuth> GetUserKeys(User user)
        {
            return _context.Users.Where(x => x.Id == user.Id).SelectMany(x => x.ApiKeys);
        }
    }
}