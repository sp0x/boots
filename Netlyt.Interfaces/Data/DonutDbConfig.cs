using System.Collections.Generic;

namespace Netlyt.Interfaces.Data
{
    public class DonutDbConfig : IDatabaseConfiguration
    {
        private static Dictionary<string, DonutDbConfig> _databases = new Dictionary<string, DonutDbConfig>();

        public DonutDbConfig(string name, string role, string value, DatabaseType type = DatabaseType.MongoDb)
        {
            Name = name;
            Role = role;
            Value = value;
            Type = type;
            RegisterConfig(this);
        }

        public string Name { get; set; }
        public string Role { get; set; }
        public string Value { get; set; }
        public DatabaseType Type { get; set; }
        public string GetUrl()
        {
            return Value;
        }

        private static void RegisterConfig(DonutDbConfig cfg)
        {
            _databases[cfg.Role] = cfg;
        }
        public static DonutDbConfig GetConfig(string role="general")
        {
            DonutDbConfig dbc;
            if (!_databases.TryGetValue(role, out dbc))
            {
                return null;
            }
            return dbc;
        }

        public static DonutDbConfig GetOrAdd(string role, DonutDbConfig toDonutDbConfig)
        {
            if (_databases.ContainsKey(role)) return _databases[role];
            else
            {
                RegisterConfig(toDonutDbConfig);
                _databases[toDonutDbConfig.Role] = toDonutDbConfig;
                return toDonutDbConfig;
            }
        }
    }
}
