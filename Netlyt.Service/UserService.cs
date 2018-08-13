using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Donut.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Netlyt.Data;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Models.Account;
using DataIntegration = Donut.Data.DataIntegration;

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
        private ManagementDbContext _context;

        public UserService(UserManager<User> userManager,
            ApiService apiService,
            ILoggerFactory lfactory,
            IHttpContextAccessor contextAccessor,
            OrganizationService orgService,
            ModelService modelService,
            ManagementDbContext context)
        {
            if (lfactory != null) _logger = lfactory.CreateLogger("Netlyt.Service.UserService");
            _userManager = userManager;
            _apiService = apiService;
            _contextAccessor = contextAccessor;
            _orgService = orgService;
            _modelService = modelService;
            _context = context;
        }

        public IdentityResult CreateUser(RegisterViewModel model, out User user)
        {
            var username = model.Email.Substring(0, model.Email.IndexOf("@"));
            user = new User
            {
                UserName = username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                RateLimit = ApiRateLimit.CreateDefault()
            };
            var org = _orgService.Get(model.Org);
            if (org == null)
                user.Organization = new Organization()
                {
                    Name = model.Org
                };
            var apiKey = _apiService.Generate();
            user.ApiKeys.Add(new ApiUser(user, apiKey));
            var result = _userManager.CreateAsync(user, model.Password).Result;
            return result;
        }
        public async Task CreateUser(User model, string password, ApiAuth appAuth)
        {
            var apiKey = _apiService.Generate();
            model.ApiKeys.Add(new ApiUser(model, apiKey));
            model.ApiKeys.Add(new ApiUser(model, appAuth));
            _context.Users.Add(model);
            _context.SaveChanges();
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
                if (keys != null) userModel.ApiKeys = keys?.ToList();
            }
            return userModel;
        }

        public async Task<User> GetCurrentUser()
        {
            var userClaim = _contextAccessor.HttpContext.User;
            var userObj = await GetUser(userClaim);
            return userObj;
        }

        public async Task<IEnumerable<Model>> GetMyModels(int page, int pageSize = 25)
        {
            var myuser = await this.GetCurrentUser();
            return _modelService.GetAllForUser(myuser, page, pageSize);
        }

        public async Task DeleteModel(long id)
        {
            var cruser = await GetCurrentUser();
            _modelService.DeleteModel(cruser, id);
        }

        /// <summary>
        /// Gets the current user's first api key.
        /// </summary>
        /// <returns></returns>
        public async Task<ApiAuth> GetCurrentApi() 
        {
            var crUser = await GetCurrentUser();
            if (crUser == null) return null;
            var api = crUser.ApiKeys.ToList().FirstOrDefault();
            if (api == null) return null;
            var apiObj = _context.ApiKeys.FirstOrDefault(x=>x.Id == api.ApiId);
            return apiObj;
        }

        public async Task<IEnumerable<DataIntegration>> GetIntegrations(User user, int page, int pageSize)
        {
            var integrations = _context.Integrations.Where(x => x.Owner == user).Skip(page * pageSize).Take(pageSize)
                .ToList();
            return await Task.FromResult(integrations);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataIntegration GetUserIntegration(User user, string name)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            var integration = _context.Integrations.FirstOrDefault(x => x.APIKey != null
                                                                        && (user.ApiKeys.Any(y => y.ApiId == x.APIKey.Id)
                                                                            || user.ApiKeys.Any(y => y.ApiId == x.PublicKeyId))
                                                                        && x.Name == name);
            return integration;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataIntegration GetUserIntegration(User user, long id)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            var integration = _context.Integrations.FirstOrDefault(x => x.APIKey != null
                                                                        && (user.ApiKeys.Any(y => y.ApiId == x.APIKey.Id)
                                                                            || user.ApiKeys.Any(y => y.ApiId == x.PublicKeyId))
                                                                        && x.Id == id);
            return integration;
        }


        public User GetByApiKey(ApiAuth appAuth)
        {
            return _context.Users
                .Include(x => x.ApiKeys)
                .FirstOrDefault(u => u.ApiKeys.Any(x => x.ApiId == appAuth.Id));
        }

        public void SetUserEmail(User user, string newEmail)
        {
            user.Email = newEmail;
            _context.SaveChanges();
        }

        public User GetUsername(string modelEmail)
        {
            return _context.Users.FirstOrDefault(x => x.Email == modelEmail);
        }
    }
}