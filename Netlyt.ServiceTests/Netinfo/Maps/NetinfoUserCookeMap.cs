using Donut.Caching;
using nvoid.db.Caching;

namespace Netlyt.ServiceTests.Netinfo.Maps
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