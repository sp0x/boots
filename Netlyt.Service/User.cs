using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using nvoid.Integration;
using Netlyt.Service.Ml;

namespace Netlyt.Service
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
        public Organization Organization { get; set; }
        public virtual ICollection<ApiUser> ApiKeys { get; set; }
        public UserRole Role { get; set; }

        public User()
        {
            ApiKeys = new HashSet<ApiUser>();
        }

//        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<User> manager)
//        {
//            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
//            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
//            // Add custom user claims here
//            return userIdentity;
//        }
    }
}