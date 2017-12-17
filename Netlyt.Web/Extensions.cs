using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Netlyt.Web
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Retrieve the raw body as a string from the Request.Body stream
        /// </summary>
        /// <param name="request">Request instance to apply to</param>
        /// <param name="encoding">Optional - Encoding, defaults to UTF8</param>
        /// <returns></returns>
        public static async Task<string> GetRawBodyString(this HttpRequest request, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            using (StreamReader reader = new StreamReader(request.Body, encoding))
                return await reader.ReadToEndAsync();
        }
    }
}
