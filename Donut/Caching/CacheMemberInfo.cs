using System.Reflection;
using StackExchange.Redis;

namespace Donut.Caching
{
    public class CacheMemberInfo
    {
        public MemberInfo Member { get; set; }
        public string Prefix { get; private set; }

        public CacheMemberInfo(string pfx, MemberInfo info)
        {
            Prefix = pfx;
            if (Prefix == null) Prefix = "";
            Member = info;
        }

        public RedisValue GetName()
        {
            return $"{Prefix}{Member.Name}";
        }
         
    }
}