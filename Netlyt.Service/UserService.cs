using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using nvoid.Integration;
using Netlyt.Data;
using Netlyt.Service.Ml;
using Netlyt.Service.Models.Account; 

namespace Netlyt.Service
{
    public class UserService
    {
        private ILogger _logger;
        private UserManager<User> _userManager;
        private ApiService _apiService;
        private IHttpContextAccessor _contextAccessor;
        private OrganizationService _orgService;
        private ModelService _modelService;

        public UserService(UserManager<User> userManager, 
            ApiService apiService,
            ILoggerFactory lfactory,
            IHttpContextAccessor contextAccessor,
            OrganizationService orgService,
            ModelService modelService)
        {
            _logger = lfactory.CreateLogger("Netlyt.Service.UserService");
            _userManager = userManager;
            _apiService = apiService;
            _contextAccessor = contextAccessor;
            _orgService = orgService;
            _modelService = modelService;
        }
        
        public IdentityResult CreateUser(RegisterViewModel model, out User user)
        {
            var username = model.Email.Substring(0, model.Email.IndexOf("@"));
            user = new User
            {
                UserName = username, Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };
            var org = _orgService.Get(model.Org);
            if (org == null)
                user.Organization = new Organization()
                {
                    Name = model.Org
                };
            var apiKey = _apiService.Generate();
            user.ApiKeys.Add(apiKey);
            var result = _userManager.CreateAsync(user, model.Password).Result;
            return result;
        }

        public AuthenticationTicket ValidateHmacSession()
        {
            var user = _contextAccessor.HttpContext.User;
            var identity2 = user.Identity;
            // success! Now we just need to create the auth ticket
            var identity = new ClaimsIdentity(AuthenticationSchemes.DataSchemes); // the name of our auth scheme
            var principal = new ClaimsPrincipal(identity);
            // you could add any custom claims here
            //var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), null, "apikey"); 
             
            var authProps = new AuthenticationProperties();
            var ticket = new AuthenticationTicket(principal, authProps, AuthenticationSchemes.DataSchemes);
            return ticket;
        }
        public async Task<AuthenticationTicket> InitializeHmacSession()
        {
            var userAccount = _apiService.GetCurrentApiUser();
            var appApiIdStr = _contextAccessor.HttpContext.Session.GetString("APP_API_ID");
            var userClaims = new List<Claim>();
            userClaims.Add(new Claim(ClaimTypes.Name, userAccount.UserName));
            userClaims.Add(new Claim("ApiId", appApiIdStr));
            userClaims.Add(new Claim(ClaimTypes.Email, userAccount.Email));

            var claimsIdentity = new ClaimsIdentity(userClaims, AuthenticationSchemes.DataSchemes);
            var principal = new ClaimsPrincipal(claimsIdentity);
            var signInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            await _contextAccessor.HttpContext.SignInAsync(principal: principal, scheme: signInScheme);
            _contextAccessor.HttpContext.User.AddIdentity(claimsIdentity);
            var signedIn = _contextAccessor.HttpContext.User.Identity.IsAuthenticated;
            var authProps = new AuthenticationProperties()
            {
                IsPersistent = true
            };
            var ticket = new AuthenticationTicket(principal, authProps, AuthenticationSchemes.DataSchemes);
            return ticket;
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

        public async Task<User> GetUser(ClaimsPrincipal user)
        {
            var userModel = await _userManager.GetUserAsync(user);
            if (userModel != null)
            {
                var keys = _apiService.GetUserKeys(userModel);
                if (keys!=null) userModel.ApiKeys = keys?.ToList();
            }
            return userModel;
        }

        public async Task<User> GetCurrentUser()
        {
            var userClaim = _contextAccessor.HttpContext.User;
            var userObj = await GetUser(userClaim);
            return userObj;
        }

        public async Task<IEnumerable<Model>> GetMyModels(int page)
        {
            var myuser = await this.GetCurrentUser();
            return _modelService.GetAllForUser(myuser, page);
        }

        public async Task DeleteModel(long id)
        {
            var cruser = await GetCurrentUser();
            _modelService.DeleteModel(cruser, id);
        }
    }
}