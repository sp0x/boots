using System.IO;
using Microsoft.Net.Http.Headers;
using Netlyt.Service.Integration;

namespace Netlyt.Interfaces
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