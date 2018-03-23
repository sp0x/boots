using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MongoDB.Bson;
using MongoDB.Driver;
using nvoid.db.Batching;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.db.Extensions;
using Netlyt.Service.FeatureGeneration;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.Models;

namespace Netlyt.Service.Donut
{
    public interface IDonutRunner
    {
        Task<HarvesterResult> Run(IDonutfile donut, FeatureGenerator<IntegratedDocument> featureGenerator);
    }
    public interface IDonutRunner<TDonut>  : IDonutRunner
    {
        Task<HarvesterResult> Run(TDonut donut, FeatureGenerator<IntegratedDocument> getFeatureGenerator);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDonut"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class DonutRunner<TDonut, TContext> : IDonutRunner<TDonut>
        where TContext: DonutContext
        where TDonut : Donutfile<TContext>
    {
        private Harvester<IntegratedDocument> _harvester;
        private IMongoCollection<BsonDocument> _featuresCollection;
        private IPropagatorBlock<IntegratedDocument, FeaturesWrapper<IntegratedDocument>> _featuresBlock;

        public DonutRunner(Harvester<IntegratedDocument> harvester, DatabaseConfiguration db, string featuresCollection)
        {
            if (string.IsNullOrEmpty(featuresCollection))
            {
                throw new ArgumentNullException(nameof(featuresCollection));
            }
            _harvester = harvester;
            var mlist = new MongoList(db, featuresCollection);
            _featuresCollection = mlist.Records;
        }

        public async Task<HarvesterResult> Run(IDonutfile donut, FeatureGenerator<IntegratedDocument> featureGenerator)
        {
            return await Run(donut as TDonut, featureGenerator);
        }
        public async Task<HarvesterResult> Run(TDonut donut, FeatureGenerator<IntegratedDocument> getFeatureGenerator)
        {
            var integration = donut.Context.Integration;
            var donutBlock = donut.CreateDataflowBlock(getFeatureGenerator);
            var flowBlock = donutBlock.FlowBlock;
            _featuresBlock = donutBlock.FeaturePropagator;

            var insertCreator = new TransformBlock<FeaturesWrapper<IntegratedDocument>, BsonDocument>((x) =>
            { 
                var rawFeatures = new BsonDocument();
                var featuresDocument = new IntegratedDocument(rawFeatures);
                //add some cleanup, or feature document definition, because right now the original document is used
                //either clean  it up or create a new one with just the features.
                //if (doc.Document.Value.Contains("events")) doc.Document.Value.Remove("events");
                //if (doc.Document.Value.Contains("browsing_statistics")) doc.Document.Value.Remove("browsing_statistics");
                foreach (var featurePair in x.Features)
                {
                    var name = featurePair.Key;
                    if (string.IsNullOrEmpty(name)) continue;
                    var featureval = featurePair.Value;
                    rawFeatures.Set(name, BsonValue.Create(featureval));
                } 
                featuresDocument.IntegrationId = integration.Id;
                featuresDocument.APIId = integration.APIKey.Id;
                x.Features = null;
                return rawFeatures;
            });
            var insertBatcher = new MongoInsertBatch<BsonDocument>(_featuresCollection, 3000);
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
            if (donut.ReplayInputOnFeatures && !donut.SkipFeatureExtraction)
            {
                var featuresFlow = new InternalFlowBlock<IntegratedDocument, FeaturesWrapper<IntegratedDocument>>
                    (_featuresBlock);
                _harvester.Reset();
                _harvester.SetDestination(featuresFlow);
                var featuresResult = await _harvester.Run(); 
            }
            else
            {
                if (!donut.SkipFeatureExtraction)
                {

                }
            }

            try
            {
                await donut.OnFinished();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Donut error: " + ex.Message);
            }
        }

    }
}