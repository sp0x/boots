using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.Batching;
using nvoid.db.Extensions;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.Models;

namespace Netlyt.Service.Donut
{
    public class DonutRunner<TDonut, TContext>
        where TContext: DonutContext
        where TDonut : Donutfile<TContext>
    {
        private Harvester<IntegratedDocument> _harvester;
        private IMongoCollection<IntegratedDocument> _documentStore;
        private IPropagatorBlock<IntegratedDocument, FeaturesWrapper<IntegratedDocument>> _featuresBlock;

        public DonutRunner(Harvester<IntegratedDocument> harvester)
        {
            _harvester = harvester;
            _documentStore = typeof(IntegratedDocument).GetDataSource<IntegratedDocument>().AsMongoDbQueryable();
        }

        public async Task<HarvesterResult> Run(TDonut donut, FeatureGenerator<IntegratedDocument> ftrGenerator)
        {
            var integration = donut.Context.Integration;
            var donutBlock = donut.CreateDataflowBlock(ftrGenerator);
            var flowBlock = donutBlock.FlowBlock;
            _featuresBlock = donutBlock.FeaturePropagator;

            var insertCreator = new TransformBlock<FeaturesWrapper<IntegratedDocument>, IntegratedDocument>((x) =>
            {
                var doc = x.Document;
                //Cleanup
                doc.Document.Value.Remove("events");
                doc.Document.Value.Remove("browsing_statistics");
                foreach (var featurePair in x.Features)
                {
                    var name = featurePair.Key;
                    if (string.IsNullOrEmpty(name)) continue;
                    var featureval = featurePair.Value;
                    doc.Document.Value.Set(name, BsonValue.Create(featureval));
                }
                //Cleanup
                doc.IntegrationId = integration.Id; doc.APIId = integration.APIKey.Id;
                x.Features = null;
                return doc;
            });
            var insertBatcher = new MongoInsertBatch<IntegratedDocument>(_documentStore, 3000);
            insertCreator.LinkTo(insertBatcher.BatchBlock, new DataflowLinkOptions { PropagateCompletion = true });

            _featuresBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            flowBlock.ContinueWith(() =>
            {
                var extractionTask = RunFeatureExtraction(donut);
                Task.WaitAll(extractionTask);
            }); 
            _harvester.SetDestination(flowBlock);
            
            var harvesterRun = await _harvester.Run(); 
            //If we have to repeat it, handle this..
            return harvesterRun;
        }

        private async Task RunFeatureExtraction(TDonut donut)
        {
            donut.Complete();
            if (donut.ReplayInputOnFeatures)
            {
                var featuresFlow = new InternalFlowBlock<IntegratedDocument, FeaturesWrapper<IntegratedDocument>>(_featuresBlock);
                _harvester.Reset();
                _harvester.SetDestination(featuresFlow);
                var featuresResult = await _harvester.Run();
                featuresResult = featuresResult;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}