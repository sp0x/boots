using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Cloud
{
    public class Errors
    {
        public static byte[] AuthorizationFailed(Exception ex)
        {
            return Encode(JObject.FromObject(new
            {
                success=false,
                message="Authorization failed.",
                reason="Server error."
            }));
        }

        private static byte[] Encode(JObject obj)
        {
            var str = obj.ToString();
            var output = Encoding.UTF8.GetBytes(str);
            return output;
        }
    }
}
