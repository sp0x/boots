using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;

namespace Netlyt.Web.Middleware
{
    public static class EnableRequestRewindExtension
    {
        public static IApplicationBuilder UseEnableRequestRewind(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnableRequestRewindMiddleware>();
        }
    }
}
