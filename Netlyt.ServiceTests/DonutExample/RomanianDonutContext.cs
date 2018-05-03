namespace Romanian
{
    using Donut;
    using Donut.Caching;
    using Netlyt.Interfaces;
    using System;
    using MongoDB.Bson;

    public class RomanianDonutContext : DonutContext
    {
        public CacheSet<string> f_0 { get; set; }
        public CacheSet<string> f_1 { get; set; }




        public RomanianDonutContext(RedisCacher cacher, DataIntegration intd, IServiceProvider serviceProvider)
            : base(cacher, intd, serviceProvider)
        {
        }

        protected override void ConfigureCacheMap()
        {

        }

    }
}