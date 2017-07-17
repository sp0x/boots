using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Peeralize.Controllers;

namespace Peeralize.Middleware.Hmac
{
    public class HmacMiddleware : AuthenticationMiddleware<HmacOptions>
    {
        private readonly IMemoryCache _memoryCache;
        public HmacMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            IOptions<HmacOptions> options,
            IMemoryCache memoryCache)
            : base(next, options, loggerFactory, encoder)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _memoryCache = memoryCache;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override AuthenticationHandler<HmacOptions> CreateHandler()
        {
            return new HmacHandler(_memoryCache);
        }
    }
}