using Microsoft.AspNetCore.Builder;

namespace Peeralize.Middleware.Hmac
{
    public class HmacOptions : AuthenticationOptions
    {
        public ulong MaxRequestAgeInSeconds { get; set; }

        public HmacOptions()
        {
            MaxRequestAgeInSeconds = HmacAuthenticationDefaults.MaxRequestAgeInSeconds;
            AuthenticationScheme = HmacAuthenticationDefaults.AuthenticationScheme;
        }
    }
}