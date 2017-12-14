using System;
using nvoid.db.DB;

namespace Netlyt.Web.Models.DataModels{
    public class User: Entity
    {
        public long ID {get; set;}
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Password { get; set; }
        public string Email { get; set; }
        public Organization Organization { get; set; }

    }
}