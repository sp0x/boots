using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        public static IApplicationBuilder UseHmacAuthentication(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<HmacMiddleware>();
        }

        public static IApplicationBuilder UseHmacAuthentication(this IApplicationBuilder app, HmacOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<HmacMiddleware>(Options.Create(options));
        }
    }
}