using System;
using System.Collections.Generic;
using Netlyt.Interfaces;

namespace Donut.FeatureGeneration
{
    public class FeatureGeneratorFactory<TData>
    where TData : class, IIntegratedDocument
    {
        public static IFeatureGenerator<TData> Create(IDonutFeatureEmitter<TData> emitter) 
        {
            var generator = new FeatureGenerator<TData>(
                new Func<TData, IEnumerable<KeyValuePair<string, object>>>[]
                {
                    emitter.GetFeatures
                }, 16);
            return generator;
        }

        public static IFeatureGenerator<TData> Create(IDonutfile donut, Type donutFGen)
        {
            IDonutFeatureEmitter<TData> donutFEmitter = Activator.CreateInstance(donutFGen, donut) as IDonutFeatureEmitter<TData>;
            var generator = Create(donutFEmitter);
            return generator;
        }
    }
}