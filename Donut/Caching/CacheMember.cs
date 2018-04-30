using System.Reflection;
using System.Text;
using StackExchange.Redis;

namespace Donut.Caching
{
    public class CacheMember
    {
        private PropertyInfo _property;
        private string _prefix;
        /// <summary>
        /// The cache key that's used to store the member.
        /// </summary>
        public string Key { get; private set; }
        public PropertyInfo Property => _property;
        public CacheMember(string prefix, PropertyInfo prop)
        {
            _property = prop;
            _prefix = prefix;
            int sbLen = prop.Name.Length + _prefix.Length;
            var sb = new StringBuilder(sbLen);
            sb.Append(_prefix).Append(prop.Name);
            Key = sb.ToString();
        }

        public object GetValue(object context)
        {
            return _property.GetValue(context);
        }
        

        public string GetSubKey(string subKey)
        {
            var sb = new StringBuilder(subKey.Length + _prefix.Length);
            sb.Append(_prefix).Append(subKey);
            return sb.ToString();
        }

        public RedisValue GetName()
        {
            return $"{_prefix}{_property.Name}";
        }

        public void SetKey(string key)
        {
            Key = key;
        }
    }
}