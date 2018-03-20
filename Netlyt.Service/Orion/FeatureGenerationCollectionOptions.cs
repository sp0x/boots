using System;

namespace Netlyt.Service.Orion
{
    public class FeatureGenerationCollectionOptions
    {
        public string Name { get; set; }
        public string Collection { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string IndexBy { get; set; }
    }
}