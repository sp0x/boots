using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Netlyt.Web.Middleware.Hmac
{
    public static class Extensions
    {
        public static long GetUserApiId(this ISession session)
        {
            var apiId = session.GetString("APP_API_ID");
            return long.Parse(apiId);
        }

        public static AuthenticationBuilder AddHmacAuthentication(this IServiceCollection services)
        {
            var dataScheme = Netlyt.Data.AuthenticationSchemes.DataSchemes;
            return services.AddAuthentication()
                .AddScheme<HmacOptions, HmacHandler>(dataScheme, dataScheme,
                (HmacOptions options) =>
                { 
                    options.ClaimsIssuer = dataScheme;
                })
                .AddCookie(options =>
                {
                    options.AccessDeniedPath = new PathString("/user/AccessDenied");
                    options.LoginPath = new PathString("/user/Login");
                });
        }
    }
}