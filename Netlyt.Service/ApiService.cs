using System.Linq;
using System.Threading.Tasks;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Http;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Repisitories;

namespace Netlyt.Service
{
    public class ApiService
    { 
        private IHttpContextAccessor _contextAccessor;
        private IApiKeyRepository _apiKeyRepisitory;
        private IDbContextScopeFactory _dbContextFactory;

        public ApiService(
            IHttpContextAccessor contextAccessor,
            IApiKeyRepository apiKeyRepository,
            IDbContextScopeFactory dbScopeFactory)
        {
            _apiKeyRepisitory = apiKeyRepository;
            _contextAccessor = contextAccessor;
            _dbContextFactory = dbScopeFactory;
        }

        public ApiAuth GetApi(long userApiId)
        {
            //using (var context = _factory.Create())
            //{
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var api = context.ApiKeys.Find(userApiId);
                return api;
            }
            //}
        }
        public ApiAuth GetApi(string appId)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var api = context.ApiKeys.FirstOrDefault(x => x.AppId == appId);
                return api;
            }
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
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var appApiIdStr = _contextAccessor.HttpContext.Session.GetString("APP_API_ID");
                if (string.IsNullOrEmpty(appApiIdStr)) return null;
                var id = long.Parse(appApiIdStr);
                var user = context.Users.FirstOrDefault(x => x.ApiKeys.Any(a => a.ApiId == id));
                return user;
            }
        }
//        public async Task<ApiAuth> GetCurrentApi()
//        {
//            var appApiIdStr = _contextAccessor.HttpContext.Session.GetString("APP_API_ID");
//            if (string.IsNullOrEmpty(appApiIdStr))
//            {
//                var crUser = await _userService.GetCurrentUser();
//                var userApi = crUser?.ApiKeys.FirstOrDefault();
//                if (userApi != null)
//                {
//                    _contextAccessor.HttpContext.Session.SetString("APP_API_ID", userApi.Id.ToString());
//                    return userApi;
//                }
//                return null;
//            }
//            var id = long.Parse(appApiIdStr);
//            return GetApi(id);
//        }
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
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var user = context.Users.FirstOrDefault(x => x.ApiKeys.Any(a => a.ApiId == api.Id));
                return user;
            }
        }

        public ApiAuth Create(string appId)
        {
            var auth = ApiAuth.Generate();
            auth.AppId = appId;
            Register(auth);
            return auth;
        }
        public void Register(ApiAuth key)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                context.ApiKeys.Add(key);
                context.SaveChanges();
            }
        }
        public async Task RegisterAsync(ApiAuth key)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                context.ApiKeys.Add(key);
                await context.SaveChangesAsync();
            }
        }

        public void RemoveKey(ApiAuth appId)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                context.ApiKeys.Remove(appId);
                context.SaveChanges();
            }
        }

        public IQueryable<ApiUser> GetUserKeys(User user)
        {
            return _apiKeyRepisitory.GetForUser(user);
            //return _context.Users.Where(x => x.Id == user.Id).SelectMany(x => x.ApiKeys);
        }
    }
}