using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Peeralize.Middleware.Hmac
{
    public static class Extensions
    {
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
