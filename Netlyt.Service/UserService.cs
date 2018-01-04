using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using nvoid.Integration;
using Netlyt.Service.Models.Account; 

namespace Netlyt.Service
{
    public class UserService
    {
        private ILogger _logger;
        private UserManager<User> _userManager;
        private ApiService _apiService;
        private IHttpContextAccessor _contextAccessor;

        public UserService(UserManager<User> userManager, 
            ApiService apiService,
            ILoggerFactory lfactory,
            IHttpContextAccessor contextAccessor)
        {
            _logger = lfactory.CreateLogger("Netlyt.Service.UserService");
            _userManager = userManager;
            _apiService = apiService;
            _contextAccessor = contextAccessor;
        }

        public IdentityResult CreateUser(RegisterViewModel model, out User user)
        {
            var username = model.Email.Substring(0, model.Email.IndexOf("@"));
            user = new User {  UserName = username, Email = model.Email };
            var apiKey = _apiService.Generate();
            user.ApiKeys.Add(apiKey);
            var result = _userManager.CreateAsync(user, model.Password).Result;
            return result;
        }

        public ClaimsPrincipal InitializeHmacSession(ApiAuth apiAuth)
        {
            var claimsIdentity = new ClaimsIdentity("HMAC");
            var principal = new ClaimsPrincipal(claimsIdentity); 
            var user = _contextAccessor.HttpContext.User;
            _apiService.SetCurrentApi(apiAuth);
            user.AddIdentity(claimsIdentity);
            return principal;
            //            var appApiId = HostingApplication.Context.Session.GetString("APP_API_ID");
            //            if (appApiId == null)
            //            {
            //                Context.Session.SetString("APP_API_ID", apiAuth.Id.ToString());
            //                user.AddIdentity(claimsIdentity);
            //            }
            //            Response.Headers.Add("APP_API_ID", apiAuth.Id.ToString());
        }

        public void InitializeUserSession(ClaimsPrincipal httpContextUser)
        {
            var appApiId = _contextAccessor.HttpContext.Session.GetString("APP_API_ID");
            if (appApiId == null)
            {
                //_contextAccessor.HttpContext.Session.SetString("APP_API_ID", apiAuth.Id.ToString());
                //user.AddIdentity(claimsIdentity);
            }
            //_contextAccessor.HttpContext.Response.Headers.Add("APP_API_ID", apiAuth.Id.ToString());
            //            var claims = new List<Claim>
            //            {
            //                new Claim(ClaimTypes.Name, loginModel.Username)
            //            };
            //
            //            var userIdentity = new ClaimsIdentity(claims, "login");
            //ClaimsPrincipal principal = new ClaimsPrincipal(httpContextUser);
            var isAuthed = _contextAccessor.HttpContext.User.Identity.IsAuthenticated;
            //await _contextAccessor.HttpContext.SignInAsync(httpContextUser);
        }
    }
}