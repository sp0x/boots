using nvoid.db.Caching;
using Netlyt.Service.Models.Netinfo;

namespace Netlyt.Service.Models.CacheMaps
{
    public class NetinfoUserCookeMap : CacheMap<NetinfoUserCookie>
    {
        public override void Map()
        {
            AddMember(x => x.Uuid);
            AddMember(x => x.Age);
            AddMember(x => x.Gender);
        }
    }
}