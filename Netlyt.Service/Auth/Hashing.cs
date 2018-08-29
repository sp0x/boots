using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Auth
{
    public class Hashing
    {
        public static IPasswordHasher<User> GetDefaultHasher()
        {
            return new PasswordHasher<User>();
        }
    }
}
