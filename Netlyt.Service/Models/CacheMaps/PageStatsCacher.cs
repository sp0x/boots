using nvoid.db.Caching;

namespace Netlyt.Service.Models.CacheMaps
{
    public class PageStatsCacher 
        : CacheSerializer<PageStats>
    {
        public override void Map()
        {
            AddMember(x => x.PurchasedUsers).Merge((a,b)=> a.PurchasedUsers+=b.PurchasedUsers)
                .AddMember(x => x.PageVisitsTotal)
                .Merge((a,b) => a.PageVisitsTotal += b.PageVisitsTotal)
                .AddMember(x => x.TotalTransitionDuration.TotalSeconds)
                .Merge((a,b)=> a.TotalTransitionDuration += b.TotalTransitionDuration);
        }
    }
}