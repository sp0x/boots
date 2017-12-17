
using Microsoft.AspNetCore.Identity;
using nvoid.db.DB;
//http://www.binaryintellect.net/articles/b957238b-e2dd-4401-bfd7-f0b8d984786d.aspx
namespace Netlyt.Web.Models.DataModels
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