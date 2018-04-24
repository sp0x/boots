using System;

namespace Netlyt.Interfaces
{
    public interface IDistributedCacheEntryOptions
    {
        DateTimeOffset? AbsoluteExpiration { get; set; }
        TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        TimeSpan? SlidingExpiration { get; set; }
    }
}