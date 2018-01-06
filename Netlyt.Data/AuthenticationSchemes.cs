using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Netlyt.Data
{
    public static class AuthenticationSchemes
    {
        public const string DataSchemes = "Hmac";
        public const string ApiSchemes = DataSchemes + ","
            + CookieAuthenticationDefaults.AuthenticationScheme;
    }
}
