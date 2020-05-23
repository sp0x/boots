using System;
using MongoDB.Driver;

namespace Netlyt.Data
{
    public class DatabaseConfiguration
        // : ConfigurationElement, IConfigElement
    {
        //[ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name { get; set; } //=> {get}(string)base["name"];

        //[ConfigurationProperty("role", IsRequired = false, IsKey = true)]
        public string Role { get; set; }//=> (string)base["role"];

        /// <summary>
        /// The url which will be used to connect with the database entry.
        /// </summary>
        //[ConfigurationProperty("value", IsRequired = true, IsKey = false)]
        public string Value { get; set; }//=> (string)base["value"];

//        //[ConfigurationProperty("db", IsRequired = true, IsKey = false)]
//        public string Database { get; set; } //=> (string)base["db"];

        //[ConfigurationProperty("db_type", IsRequired = true, IsKey = false)]
        public DatabaseType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetDatabaseName()
        {
            switch (Type)
            {
                case DatabaseType.MongoDb:
                    var urlValue = MongoUrl.Create(Value);
                    return urlValue.DatabaseName; 
                default:
                    throw new NotImplementedException(); 
            }
        }
    }
}