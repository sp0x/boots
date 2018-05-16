using System;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;

namespace Donut
{
    public abstract class DonutRunnerFactory
    {
        /// <summary>
        /// Creates a new donut runner
        /// </summary>
        /// <typeparam name="TDonut">The type of donut to run</typeparam>
        /// <typeparam name="TContext">The donut's type of context</typeparam>
        /// <typeparam name="TData">The type of data that will be consumed</typeparam>
        /// <param name="harvester">The harvester that will read the data</param>
        /// <param name="db">Db config</param>
        /// <param name="featuresCollection">The targeted features collection that would be outputed to.</param>
        /// <returns></returns>
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
                    typeof(Harvester<TData>),
                    typeof(IDatabaseConfiguration),
                    typeof(string) 
                });
            if (builderCtor == null) throw new Exception("DonutBuilder<> has invalid ctor parameters.");
            var builder = Activator.CreateInstance(builderType, harvester, db, featuresCollection) as IDonutRunner<TDonut, TData>;
            return builder;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="donutType"></param>
        /// <param name="donutContextType"></param>
        /// <param name="harvester"></param>
        /// <param name="db"></param>
        /// <param name="featuresCollection"></param>
        /// <returns></returns>
        public static IDonutRunner<IntegratedDocument> CreateByType(Type donutType,
            Type donutContextType,
            Harvester<IntegratedDocument> harvester,
            IDatabaseConfiguration db, 
            string featuresCollection)
        {
            var runnerCrMethod = typeof(DonutRunnerFactory).GetMethod(nameof(DonutRunnerFactory.Create));
            var runnerCtor = runnerCrMethod.MakeGenericMethod(donutType, donutContextType, typeof(IntegratedDocument));
            var runner = runnerCtor.Invoke(null, new object[] { harvester, db, featuresCollection });
            return runner as IDonutRunner<IntegratedDocument>;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="donutType"></param>
        /// <param name="donutContextType"></param>
        /// <param name="harvester"></param>
        /// <param name="db"></param>
        /// <param name="featuresCollection"></param>
        /// <returns></returns>
        public static IDonutRunner<TData> CreateByType<TData, THarvesterData>(Type donutType,
            Type donutContextType,
            Harvester<THarvesterData> harvester,
            IDatabaseConfiguration db,
            string featuresCollection)
            where TData : class, IIntegratedDocument 
            where THarvesterData : class
        {
            var runnerCrMethod = typeof(DonutRunnerFactory).GetMethod(nameof(DonutRunnerFactory.Create));
            var runner = runnerCrMethod.MakeGenericMethod(donutType, donutContextType, 
                typeof(TData)).Invoke(null, new object[] { harvester, db, featuresCollection });
            return runner as IDonutRunner<TData>;
        }
    }
}