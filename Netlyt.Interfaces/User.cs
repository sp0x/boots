using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Netlyt.Interfaces
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
    }
}