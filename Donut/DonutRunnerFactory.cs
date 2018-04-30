using System;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Service.Donut;

namespace Donut
{
    public abstract class DonutRunnerFactory
    {
        public static IDonutRunner<TDonut, TData> Create<TDonut, TContext, TData>(
            IHarvester<IntegratedDocument> harvester,
            IDatabaseConfiguration db,
            string featuresCollection)
            where TDonut : Donutfile<TContext, TData>
            where TContext : DonutContext
            where TData : class, IIntegratedDocument
        {
            var builderType = typeof(DonutRunner<,,>).MakeGenericType(new Type[] { typeof(TDonut), typeof(TContext), typeof(TData) });
            //DataIntegration integration, RedisCacher cacher, IServiceProvider serviceProvider
            var builderCtor = builderType.GetConstructor(new Type[]
                {
                    typeof(Harvester<IntegratedDocument>),
                    typeof(IDatabaseConfiguration),
                    typeof(string) 
                });
            if (builderCtor == null) throw new Exception("DonutBuilder<> has invalid ctor parameters.");
            var builder = Activator.CreateInstance(builderType, harvester, db, featuresCollection) as IDonutRunner<TDonut, TData>;
            return builder;
        }

        public static IDonutRunner<IntegratedDocument> CreateByType(Type donutType,
            Type donutContextType,
            Harvester<IntegratedDocument> harvester,
            IDatabaseConfiguration db, 
            string featuresCollection)
        {
            var runnerCrMethod = typeof(DonutRunnerFactory).GetMethod(nameof(DonutRunnerFactory.Create));
            var runner = runnerCrMethod.MakeGenericMethod(donutType, donutContextType, typeof(IntegratedDocument)).Invoke(null, new object[] { harvester, db, featuresCollection });
            return runner as IDonutRunner<IntegratedDocument>;
        }

        public static IDonutRunner<TData> CreateByType<TData>(Type donutType, Type donutContextType, Harvester<TData> harvester, IDatabaseConfiguration db, string featuresCollection)
            where TData : class, IIntegratedDocument
        {
            var runnerCrMethod = typeof(DonutRunnerFactory).GetMethod(nameof(DonutRunnerFactory.Create));
            var runner = runnerCrMethod.MakeGenericMethod(donutType, donutContextType, typeof(TData)).Invoke(null, new object[] { harvester, db, featuresCollection });
            return runner as IDonutRunner<TData>;
        }
    }
}