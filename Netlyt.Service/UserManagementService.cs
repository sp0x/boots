using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Netlyt.Data;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Models.Account;
using Netlyt.Service.Repisitories;

namespace Netlyt.Service
{
    public class UserManagementService : IUserManagementService
    {
        private UserManager<User> _userManager;
        private ApiService _apiService;
        private OrganizationService _orgService;
        private ManagementDbContext _context;
        private IDbContextScopeFactory _dbContextFactory;
        private IUsersRepository _userRepository;
        private IHttpContextAccessor _contextAccessor;
        public UserManagementService(
            UserManager<User> userManager,
            ApiService apiService,
            OrganizationService orgService,
            IFactory<ManagementDbContext> contextFactory,
            IDbContextScopeFactory dbFactory,
            IUsersRepository userRepository,
            IHttpContextAccessor contextAccessor)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _apiService = apiService;
            _orgService = orgService;
            _context = contextFactory.Create();
            _dbContextFactory = dbFactory;
            _contextAccessor = contextAccessor;
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
        public Task<User> GetUser(string id)
        {
            using (var contextSrc = _dbContextFactory.Create())
            {
                var result = _userRepository.GetById(id).FirstOrDefault();
                return Task.FromResult<User>(result);
            }
        }

        public IEnumerable<User> GetUsers()
        {
            return _context.Users.Include(x => x.Role);
        }
        public void SetUserEmail(User user, string newEmail)
        {
            user.Email = newEmail;
            _context.SaveChanges();
        }
        /// <summary>
        /// Gets the current user's first api key.
        /// </summary>
        /// <returns></returns>
        public async Task<ApiAuth> GetCurrentApi()
        {
            using (var contextSrc = _dbContextFactory.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var crUser = await GetCurrentUser();
                if (crUser == null) return null;
                var api = crUser.ApiKeys.ToList().FirstOrDefault();
                if (api == null) return null;
                var apiObj = context.ApiKeys.FirstOrDefault(x => x.Id == api.ApiId);
                return apiObj;
            }
        }

        public void AddUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
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

        public async Task<Tuple<IdentityResult, User>> CreateUser(RegisterViewModel model)
        {
            var isFirstUser = _context.Users.Count() == 0;
            var username = model.Email.Substring(0, model.Email.IndexOf("@"));
            var user = new User
            {
                UserName = username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                RateLimit = ApiRateLimit.CreateDefault()
            };
            var org = !string.IsNullOrEmpty(model.Org) ? _orgService.Get(model.Org) : null;
            if (org == null)
                user.Organization = new Organization()
                {
                    Name =username
                };
            var apiKey = _apiService.Generate();
            user.ApiKeys.Add(new ApiUser(user, apiKey));
            var result = _userManager.CreateAsync(user, model.Password).Result;
            if (result.Succeeded && isFirstUser)
            {
                await AddRolesToUser(user, new string[] { "Admin" });
            }
            var output = new Tuple<IdentityResult, User>(result, user);
            return output;
        }
        public async Task<bool> AddRolesToUser(User user, IEnumerable<string> newRoles)
        {
            foreach (var newRole in newRoles)
            {
                var existingRole = _context.Roles.FirstOrDefault(x => x.Name == newRole);
                if (existingRole == null)
                {
                    var newRoleObj = new UserRole()
                    {
                        Name = newRole,
                        NormalizedName = newRole.ToUpper()
                    };
                    _context.Roles.Add(newRoleObj);
                    await _context.SaveChangesAsync();
                }
                var isCurrentlyInRole = await _userManager.IsInRoleAsync(user, newRole);
                if (isCurrentlyInRole) continue;
                var idResult = await _userManager.AddToRoleAsync(user, newRole);
                if (!idResult.Succeeded)
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<IEnumerable<string>> GetRoles(User src)
        {
            var userRoles = await _userManager.GetRolesAsync(src);
            return userRoles;
        }

        public async Task<User> GetUser(ClaimsPrincipal user)
        {
            var userModel = await _userManager.GetUserAsync(user);
            using (var ctxSrc = _dbContextFactory.Create())
            {
                if (userModel != null)
                {
                    userModel = _userRepository.GetById(userModel.Id).FirstOrDefault();
                    var keys = _apiService.GetUserKeys(userModel);
                    if (keys != null) userModel.ApiKeys = keys?.ToList();
                }
            }
            return userModel;
        }
        
        public async Task<User> GetCurrentUser()
        {
            var userClaim = _contextAccessor.HttpContext.User;
            var userObj = await GetUser(userClaim);
            return userObj;
        }
    }
}