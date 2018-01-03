using System.Threading.Tasks;
using log4net.Core;
using LinqToTwitter.Net;
using Microsoft.AspNetCore.Identity;
using Netlyt.Service.Auth;
using Netlyt.Service.Models.Account;

namespace Netlyt.Service
{
    public class UserService
    {
        private ILogger _logger;
        private UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public IdentityResult CreateUser(RegisterViewModel model, out ApplicationUser user)
        {
            user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = _userManager.CreateAsync(user, model.Password).Result;
            return result;
        }
    }
}