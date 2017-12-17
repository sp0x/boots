using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Netlyt.Web.Middleware.Hmac
{
    public static class Extensions
    {
        public static string GetUserApiId(this ISession session)
        {
            var apiId = session.GetString("APP_API_ID");
            return apiId;
        }

        public static void AddHmacAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication()
                .AddScheme<HmacOptions, HmacHandler>(Netlyt.Data.AuthenticationSchemes.DataSchemes, (HmacOptions options) =>
                {
                    options = options;
                });
        }
    }
}