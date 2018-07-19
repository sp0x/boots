using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Donut;
using Donut.Data;
using Donut.FeatureGeneration;
using Donut.Lex.Data;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Service.Data;

namespace Netlyt.Service
{
    public class DonutService
    {
        public async Task<IHarvesterResult> RunExtraction(DonutScript script, DataIntegration integration, IServiceProvider serviceProvider)
        {
            var dbConfig = serviceProvider.GetService(typeof(IDatabaseConfiguration)) as IDatabaseConfiguration;
            var harvester = new Harvester<IntegratedDocument>(10);
            IInputSource source = integration.GetCollectionAsSource();

            //Create a donut and a donutRunner
            var donutMachine = DonutGeneratorFactory.Create<IntegratedDocument>(script, integration, serviceProvider);
            harvester.AddIntegration(integration, source);//source, appAuth, "SomeIntegrationName3");
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(source.Size);
            donut.ReplayInputOnFeatures = false;
            //donut.SkipFeatureExtraction = true;
            IDonutRunner<IntegratedDocument> donutRunner = DonutRunnerFactory.CreateByType<IntegratedDocument, IntegratedDocument>(
                donutMachine.DonutType,
                donutMachine.DonutContextType,
                harvester, dbConfig, integration.FeaturesCollection);
            var featureGenerator = FeatureGeneratorFactory<IntegratedDocument>.Create(donut, donutMachine.GetEmitterType());
            var result = await donutRunner.Run(donut, featureGenerator);
            return result;
        }
    }
}
