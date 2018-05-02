using System;
using System.Threading.Tasks;
using Donut.Blocks;
using Donut.Caching;
using Netlyt.Interfaces;

namespace Donut
{
    /// <summary>
    /// Base for donutfiles.
    /// A donut receives all data and processes it, extracting features as output.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public abstract class Donutfile<TContext, TData> : IDonutfile, IDisposable
        where TContext : DonutContext
        where TData : class, IIntegratedDocument
    {
        /// <summary>
        /// The data context that the donut uses.
        /// </summary>
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
        
        /// <summary>
        /// If true, all initial input is replayed in the feature extraction step.
        /// </summary>
        public bool ReplayInputOnFeatures { get; set; }
        public bool SkipFeatureExtraction { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacher"></param>
        /// <param name="serviceProvider"></param>
        public Donutfile(RedisCacher cacher, IServiceProvider serviceProvider)
        {
            //_integrationService = serviceProvider.GetService(typeof(IntegrationService)) as IntegrationService;
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

        /// <summary>
        /// Processes each record that has been inputed.
        /// </summary>
        /// <param name="intDoc"></param>
        public abstract void ProcessRecord(TData intDoc);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IDonutBlock<TData> CreateDataflowBlock(IFeatureGenerator<TData> featureGen)
        {
            var featuresBlock = featureGen.CreateFeaturesBlock();
            var metaBlock = new MemberVisitingBlock<TData>(ProcessRecord);
            //metaBlock.ContinueWith(RunFeatureExtraction);
            //_featuresBlock.LinkTo(insertCreator, new DataflowLinkOptions { PropagateCompletion = true });
            //insertCreator.LinkTo(insertBatcher.BatchBlock, new DataflowLinkOptions { PropagateCompletion = true });
            //metaBlock.AddCompletionTask(featuresBlock.Completion);
            //var metaBlockInternal = metaBlock.GetInputBlock();
            //Encapsulate our input and features block, so they're usable.
            //var resultingBlock = DataflowBlock.Encapsulate<IntegratedDocument, FeaturesWrapper<IntegratedDocument>>(metaBlockInternal, featuresBlock);
            return new DonutBlock<TData>(metaBlock, featuresBlock);
        }

        public void Complete()
        {
            Context.Complete();
            OnMetaComplete();
        }

        public async virtual Task PrepareExtraction()
        {

        }

        public async virtual Task OnFinished()
        {

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
