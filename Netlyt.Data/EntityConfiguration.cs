namespace Netlyt.Data
{
    public class EntityConfiguration// : ConfigurationElement, IConfigElement
    {
        //[ConfigurationProperty("entity", IsRequired = true, IsKey = true)]
        public string Entity { get; set; }// => (string)base["entity"];

        //[ConfigurationProperty("absolute", IsRequired = false, IsKey = false)]
        public bool Absolute { get; set; }// => (bool)base["absolute"];
          
        public EntityConfiguration() { }
         

        public override string ToString()
        {
            return Entity;
        }
    }
}