namespace Netlyt.Service.Orion
{
    public class FeatureGenerationRelation
    {
        public string Attribute1 { get; set; }
        public string Attribute2 { get; set; }

        public FeatureGenerationRelation(string a, string b)
        {
            Attribute1 = a;
            Attribute2 = b;
        }
    }
}