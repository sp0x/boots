using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            return services.AddAuthentication()
                .AddScheme<HmacOptions, HmacHandler>(Netlyt.Data.AuthenticationSchemes.DataSchemes, (HmacOptions options) =>
                {
                    options = options;
                });
        }
    }
}