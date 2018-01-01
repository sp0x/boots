
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.MongoDB;

//http://www.binaryintellect.net/articles/b957238b-e2dd-4401-bfd7-f0b8d984786d.aspx
namespace Netlyt.Service
{
    public class User 
        : IdentityUser
    { 
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Password { get; set; }
        public string Email { get; set; }
        public Organization Organization { get; set; }

    }

    public class UserRole : IdentityRole
    {
        
    }
}