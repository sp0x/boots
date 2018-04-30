using System;
using Netlyt.Interfaces; 

namespace Donut.Orion
{
    public class FeatureGenerationCollectionOptions
    {
        public string Name { get; set; }
        public string Collection { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string IndexBy { get; set; }
        public string TimestampField { get; set; }
        public InternalEntity InternalEntity { get; set; }
        public DataIntegration Integration { get; set; }
    }
}