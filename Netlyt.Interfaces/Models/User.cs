using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Netlyt.Interfaces.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
        public virtual Organization Organization { get; set; }
        public virtual ICollection<ApiUser> ApiKeys { get; set; }
        public virtual UserRole Role { get; set; }

        public User()
        {
            ApiKeys = new HashSet<ApiUser>();
        }
    }
}