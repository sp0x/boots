namespace Donut
{
    public interface IDatabaseConfiguration
    {
        //[ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        string Name { get; set; } //=> {get}(string)base["name"];

        //[ConfigurationProperty("role", IsRequired = false, IsKey = true)]
        string Role { get; set; }//=> (string)base["role"];

        /// <summary>
        /// The url which will be used to connect with the database entry.
        /// </summary>
        //[ConfigurationProperty("value", IsRequired = true, IsKey = false)]
        string Value { get; set; }//=> (string)base["value"];

        //        //[ConfigurationProperty("db", IsRequired = true, IsKey = false)]
        //        public string Database { get; set; } //=> (string)base["db"];

        //[ConfigurationProperty("db_type", IsRequired = true, IsKey = false)]
        DatabaseType Type { get; set; }
        string GetUrl();
    }
    /// <summary>
    /// 
    /// </summary>
    public enum DatabaseType
    {
        MongoDb, MySql
    }
}