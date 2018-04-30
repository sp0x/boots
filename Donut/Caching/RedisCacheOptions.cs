using Microsoft.Extensions.Options;

namespace Donut.Caching
{
    /// <summary>
    /// Configuration options for <see cref="RedisCache"/>.
    /// </summary>
    public class RedisCacheOptions : IOptions<RedisCacheOptions>
    {
        /// <summary>
        /// The configuration used to connect to Redis.
        /// </summary>
        public string Configuration { get; set; } 

        RedisCacheOptions IOptions<RedisCacheOptions>.Value
        {
            get { return this; }
        }
    }


}