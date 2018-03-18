using System;
using System.Text;
using System.Threading.Tasks.Dataflow;
using nvoid.db.Caching;
using nvoid.exec.Blocks;
using Netlyt.Service.FeatureGeneration;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Blocks;
using Netlyt.Service.Models;
using StackExchange.Redis;

namespace Netlyt.Service.Donut
{
    public abstract class Donutfile<TContext> : IDonutfile, IDisposable
        where TContext : DonutContext
    {
        public TContext Context
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
                OnCreated();
            }
        }
        private TContext _context;
        private IntegrationService _integrationService;

        /// <summary>
        /// If true, all initial input is replayed in the feature extraction step.
        /// </summary>
        public bool ReplayInputOnFeatures { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacher"></param>
        /// <param name="serviceProvider"></param>
        public Donutfile(RedisCacher cacher, IServiceProvider serviceProvider)
        {
            _integrationService = serviceProvider.GetService(typeof(IntegrationService)) as IntegrationService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalIntegrationSize"></param>
        public virtual void SetupCacheInterval(long totalIntegrationSize)
        {
            var interval = (int)(totalIntegrationSize * 0.10);
            Context.SetCacheRunInterval(interval);
        }

        public abstract void ProcessRecord(IntegratedDocument intDoc);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DonutBlock CreateDataflowBlock(FeatureGenerator<IntegratedDocument> featureGen)
        {
            var featuresBlock = featureGen.CreateFeaturesBlock();
            var metaBlock = new MemberVisitingBlock(ProcessRecord);
            //metaBlock.ContinueWith(RunFeatureExtraction);
            //_featuresBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            //insertCreator.LinkTo(insertBatcher.BatchBlock, new DataflowLinkOptions { PropagateCompletion = true });
            //metaBlock.AddCompletionTask(featuresBlock.Completion);
            //var metaBlockInternal = metaBlock.GetInputBlock();
            //Encapsulate our input and features block, so they're usable.
            //var resultingBlock = DataflowBlock.Encapsulate<IntegratedDocument, FeaturesWrapper<IntegratedDocument>>(metaBlockInternal, featuresBlock);
            return new DonutBlock(metaBlock, featuresBlock);
        }

        public void Complete()
        {
            Context.Complete();
            OnMetaComplete();
        }

        protected virtual void OnCreated()
        {

        }
        protected virtual void OnMetaComplete()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Context.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
/**
* TODO:
* - pass in all reduced documents to be analyzed
* - join any additional integration sources/ raw or reduced collections
* - analyze and extract metadata (variables) about the dataset
* - generate features for every constructed document (initial reduced document + any additional data) using analyzed metadata.
* -- Use redis to cache the gathered metadata from generating the required variables
* */

/**
 * Code generation style:
 * each feature generation should be a method, for readability and easy debugging/tracking
 **/
