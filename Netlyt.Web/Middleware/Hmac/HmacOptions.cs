using Microsoft.AspNetCore.Authentication;

namespace Netlyt.Web.Middleware.Hmac
{
    public class HmacOptions : AuthenticationSchemeOptions
    {
        public ulong MaxRequestAgeInSeconds { get; set; }
        public string AuthenticationScheme { get; set; }
        public bool AutomaticAuthenticate { get; set; }
        public HmacOptions()
        {
            MaxRequestAgeInSeconds = HmacAuthenticationDefaults.MaxRequestAgeInSeconds;
            AuthenticationScheme = HmacAuthenticationDefaults.AuthenticationScheme;
        }
    }
}