using System;
using System.Collections.Generic;
using Netlyt.Service.Donut;
using Netlyt.Service.Integration;

namespace Netlyt.Service.FeatureGeneration
{
    public class FeatureGeneratorFactory
    {
        public static FeatureGenerator<IntegratedDocument> Create(IDonutFeatureEmitter emitter) 
        {
            var generator = new FeatureGenerator<IntegratedDocument>(
                new Func<IntegratedDocument, IEnumerable<KeyValuePair<string, object>>>[]
                {
                    emitter.GetFeatures
                }, 16);
            return generator;
        }

        public static FeatureGenerator<IntegratedDocument> Create(IDonutfile donut, Type donutFGen)
        {
            IDonutFeatureEmitter donutFEmitter = Activator.CreateInstance(donutFGen, donut) as IDonutFeatureEmitter;
            var generator = Create(donutFEmitter);
            return generator;
        }
    }
}