using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Netlyt.Interfaces.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
        public virtual Organization Organization { get; set; }
        public virtual ICollection<ApiUser> ApiKeys { get; set; }
        [ForeignKey("RateLimit")]
        public long RateLimitId { get; set; }
        public virtual ApiRateLimit RateLimit { get; set; }
        public virtual UserRole Role { get; set; }
        public virtual IEnumerable<UserPermission> UserPermissions { get; set; }

        public User()
        {
            ApiKeys = new HashSet<ApiUser>();
            UserPermissions = new HashSet<UserPermission>();
        }
    }
}