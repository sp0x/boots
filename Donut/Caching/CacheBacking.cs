using System;
using Netlyt.Interfaces;

namespace Donut.Caching
{
    public class CacheBacking : Attribute
    {
        public CacheType Type { get; set; }
        public CacheBacking(CacheType type) { Type = type; }
    }
}