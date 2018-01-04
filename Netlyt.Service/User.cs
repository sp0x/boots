
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using nvoid.Integration;

namespace Netlyt.Service
{
    public class User 
        : IdentityUser
    { 
        public string FirstName { get; set; }

        public string LastName { get; set; }  
        public Organization Organization { get; set; } 
        public virtual ICollection<ApiAuth> ApiKeys { get; set; }
        public UserRole Role { get; set; }

        public User()
        {
            ApiKeys = new HashSet<ApiAuth>();
        }
    }
}