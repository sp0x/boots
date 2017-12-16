using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Netlyt.Web.Middleware.Hmac
{
    public class HmacMiddleware : AuthenticationMiddleware
    {
        private readonly IMemoryCache _memoryCache;

        public HmacMiddleware(RequestDelegate next, IAuthenticationSchemeProvider schemes, IMemoryCache memoryCache)
            : base(next, schemes)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            _memoryCache = memoryCache;
        }

        //        /// <summary>
        //        /// 
        //        /// </summary>
        //        /// <returns></returns>
        //        protected override AuthenticationHandler<HmacOptions> CreateHandler()
        //        {
        //            return new HmacHandler(_memoryCache);
        //        }
    }
}