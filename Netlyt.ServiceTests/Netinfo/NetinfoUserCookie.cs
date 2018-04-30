using Donut.Caching;
using nvoid.db.Caching;

namespace Netlyt.ServiceTests.Netinfo
{
    public class NetinfoUserCookie
    {
        [CacheKey]
        public string Uuid { get; set; }
        public int Age { get; set; }
        public byte Gender { get; set; }
    }
}