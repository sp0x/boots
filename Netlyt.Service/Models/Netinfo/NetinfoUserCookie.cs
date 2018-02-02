using nvoid.db.Caching;

namespace Netlyt.Service.Models.Netinfo
{
    public class NetinfoUserCookie
    {
        [CacheKey]
        public string Uuid { get; set; }
        public int Age { get; set; }
        public byte Gender { get; set; }
    }
}