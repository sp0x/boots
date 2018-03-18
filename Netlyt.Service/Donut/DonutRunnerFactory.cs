using System;
using Netlyt.Service.Integration;

namespace Netlyt.Service.Donut
{
    public abstract class DonutRunnerFactory
    {
        public static IDonutRunner<TDonut> Create<TDonut, TContext>(IHarvester<IntegratedDocument> harvester)
            where TDonut : Donutfile<TContext>
            where TContext : DonutContext
        {
            var builderType = typeof(DonutRunner<,>).MakeGenericType(new Type[] { typeof(TDonut), typeof(TContext) });
            //DataIntegration integration, RedisCacher cacher, IServiceProvider serviceProvider
            var builderCtor = builderType.GetConstructor(new Type[]
                {typeof(Harvester<IntegratedDocument>)});
            if (builderCtor == null) throw new Exception("DonutBuilder<> has invalid ctor parameters.");
            var builder = Activator.CreateInstance(builderType, harvester) as IDonutRunner<TDonut>;
            return builder;
        }

        public static IDonutRunner Create(Type donutType, Type donutContextType, Harvester<IntegratedDocument> harvester)
        {
            var runnerCrMethod = typeof(DonutRunnerFactory).GetMethod(nameof(DonutRunnerFactory.Create));
            var runner = runnerCrMethod.MakeGenericMethod(donutType, donutContextType).Invoke(null, new object[] { harvester });
            return runner as IDonutRunner;
        }
    }
}