using System.IO;
using System.Net.Mime;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Netlyt.Service.Integration
{
    public class MimeResolver
    {
        public static string Resolve(string filepath)
        {
            var extension = System.IO.Path.GetExtension(filepath);
            var mType = MimeTypeMap.GetMimeType(extension);
            return mType;
        }
        public static string Resolve(ContentDispositionHeaderValue contentDisposition)
        { 
            
            if (contentDisposition != null)
            {
                var extension = Path.GetExtension(contentDisposition.FileName.Value.Trim("\"".ToCharArray()));
                var mType = MimeTypeMap.GetMimeType(extension);
                return mType;
            }
            return null;
        }
    }
}