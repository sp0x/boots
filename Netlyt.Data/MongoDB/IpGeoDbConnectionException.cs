using System;

namespace Netlyt.Data.MongoDB
{
    public class IpGeoDbConnectionException : Exception
    {
        public Int32 Code = 0;
        public IpGeoDbConnectionException(string msg) : base(msg)
        {
        }
    }
}