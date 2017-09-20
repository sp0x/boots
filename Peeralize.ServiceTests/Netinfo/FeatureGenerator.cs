using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;

namespace Peeralize.ServiceTests.Netinfo
{
    public class DocumentFeatures
    {
        public IEnumerable<KeyValuePair<string, double>> Features { get; set; }
        public IntegratedDocument Document { get; set; }

        public DocumentFeatures(IntegratedDocument doc, IEnumerable<KeyValuePair<string, double>> features)
        {
            Document = doc;
            this.Features = features;
        }
    }

    public class FeatureGenerator 
    {
        private Func<IntegratedDocument, IEnumerable<KeyValuePair<string, double>>> _generator;
        public TransformBlock<IntegratedDocument, DocumentFeatures> Block { get; private set; }
        public FeatureGenerator(Func<IntegratedDocument, IEnumerable<KeyValuePair<string,double>>> generator)
        {
            _generator = generator;
            Block = new TransformBlock<IntegratedDocument, DocumentFeatures>(
                (x) =>
                {
                    var features = new DocumentFeatures(x, _generator(x));
                    return features;
                });
        }
    }
    
}