namespace Netlyt.Data
{
    public class UserEntry //: ConfigurationElement, IConfigElement
    {
        //[ConfigurationProperty("token", IsRequired = false, IsKey = true)]
        public string Token { get; set; }
        //[ConfigurationProperty("tokenSecret", IsRequired = false, IsKey = true)]
        public string TokenSecret { get; set; }

        // [ConfigurationProperty("type", IsRequired = false, IsKey = false)]
        public string Type { get; set; }// => (string) base["type"];
         
    }
}