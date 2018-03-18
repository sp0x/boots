using System;
using nvoid.db.DB.Configuration;
using Netlyt.Service.Integration;

namespace Netlyt.Service.Donut
{
    public abstract class DonutRunnerFactory
    {
        public static IDonutRunner<TDonut> Create<TDonut, TContext>(IHarvester<IntegratedDocument> harvester, DatabaseConfiguration db, string featuresCollection)
            where TDonut : Donutfile<TContext>
            where TContext : DonutContext
        {
            var builderType = typeof(DonutRunner<,>).MakeGenericType(new Type[] { typeof(TDonut), typeof(TContext) });
            //DataIntegration integration, RedisCacher cacher, IServiceProvider serviceProvider
            var builderCtor = builderType.GetConstructor(new Type[]
                {
                    typeof(Harvester<IntegratedDocument>),
                    typeof(DatabaseConfiguration),
                    typeof(string) 
                });
            if (builderCtor == null) throw new Exception("DonutBuilder<> has invalid ctor parameters.");
            var builder = Activator.CreateInstance(builderType, harvester, db, featuresCollection) as IDonutRunner<TDonut>;
            return builder;
        }

        public static IDonutRunner CreateByType(Type donutType, Type donutContextType, Harvester<IntegratedDocument> harvester, DatabaseConfiguration db, string featuresCollection)
        {
            var runnerCrMethod = typeof(DonutRunnerFactory).GetMethod(nameof(DonutRunnerFactory.Create));
            var runner = runnerCrMethod.MakeGenericMethod(donutType, donutContextType).Invoke(null, new object[] { harvester, db, featuresCollection });
            return runner as IDonutRunner;
        }
    }
}