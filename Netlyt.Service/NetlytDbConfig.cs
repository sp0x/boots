using System;
using System.Collections.Generic;
using System.Text;
using Donut;
using nvoid.db.DB.Configuration;

namespace Netlyt.Service
{
    public class NetlytDbConfig : IDatabaseConfiguration
    {
        public NetlytDbConfig(DatabaseConfiguration db)
        {
            Name = db.Name;
            Role = db.Role;
            Value = db.Value;
            switch (db.Type)
            {
                case nvoid.db.DB.DatabaseType.MongoDb:
                    Type = DatabaseType.MongoDb;
                    break;
                case nvoid.db.DB.DatabaseType.MySql:
                    Type = DatabaseType.MySql;
                    break;
            }
        }

        public string Name { get; set; }
        public string Role { get; set; }
        public string Value { get; set; }
        public DatabaseType Type { get; set; }
        public string GetUrl()
        {
            return Value;
        }
    }
}
