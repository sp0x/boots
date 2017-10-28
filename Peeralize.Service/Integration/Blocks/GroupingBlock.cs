using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using nvoid.extensions;
using System.Linq;
using Peeralize.Service.Models;

namespace Peeralize.Service.Integration.Blocks
{
    /// <summary>
    /// An entity grouping block
    /// </summary>
    public class GroupingBlock
        : BaseFlowBlock<IntegratedDocument, IntegratedDocument>
    {
        #region "Variables"
        private Func<IntegratedDocument, object> _groupBySelector;
        /// <summary>
        /// 
        /// </summary>
        private Func<IntegratedDocument, BsonDocument, object> _accumulator;
        /// <summary>
        /// 
        /// </summary>
        private Action<IntegratedDocument> _inputProjection;
        #endregion

        #region "Props"
//        public event EventHandler<EventArgs> GroupingComplete;

        public ConcurrentDictionary<object, IntegratedDocument> EntityDictionary { get; private set; } 

        public CrossSiteAnalyticsHelper Helper { get; set; }
        //public BsonArray Purchases { get; set; }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="selector"></param>
        /// <param name="inputProjection">Projection to perform on the input</param>
        /// <param name="accumulator">Accumulate input data to the resolved element</param>
        public GroupingBlock(string userId, Func<IntegratedDocument, object> selector,
            Action<IntegratedDocument> inputProjection,
            Func<IntegratedDocument, BsonDocument, object> accumulator)
            : base(capacity: 1000, procType: ProcessingType.Action)
        {
            base.UserId = userId;
            _groupBySelector = selector;
            this._accumulator = accumulator;
            this._inputProjection = inputProjection;
            //base.Completed += OnReadingCompleted;
            EntityDictionary = new ConcurrentDictionary<object, IntegratedDocument>();
            //PageStats = new CrossPageStats();
        } 
         

        protected override IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return EntityDictionary.Values;
        } 
        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            //Get key
            var key = _groupBySelector==null ? null : _groupBySelector(intDoc);
            var intDocDocument = intDoc.GetDocument(); 
            var isNewUser = false;
            if (key != null)
            {
                if (!EntityDictionary.ContainsKey(key))
                {
                    var docClone = intDoc;
                    //Ignore non valid values
                    if (_inputProjection != null)
                    {
                        docClone = intDoc.Clone();
                        _inputProjection(docClone);
                    }
                    EntityDictionary[key] = docClone;
                    isNewUser = true;
                }
            }
            else
            {
                throw new Exception("No key to group with!");
            }
            
            RecordPageStats(intDocDocument, isNewUser);
            var newElement = _accumulator(EntityDictionary[key], intDocDocument);
            // return EntityDictionary[key];
            return intDoc;
        }

        /// <summary>
        /// Updates page stats on every visit event
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="eventData"></param>
        /// <param name="isNewUser"></param>
        private void RecordPageStats(BsonDocument eventData, bool isNewUser)
        {
            var page = eventData["value"].ToString();
            var pageHost = page.ToHostname();
            var pageSelector = pageHost;
            var isNewPage = false;
            if (!Helper.Stats.ContainsPage(pageHost))
            {
                Helper.Stats.AddPage(pageSelector, new PageStats()
                {
                    Page = page
                });
                isNewPage = true;
            }
            if (isNewUser)
            {
                //this.PageStats[pageSelector].UsersVisitedTotal++;
            }
            if (!isNewPage)
            {
                //var duration = this.PageStats[pageSelector].VisitStarted;
            }
            Helper.Stats[pageSelector].PageVisitsTotal++;
        }
         

        /// <summary>
        /// 
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public Task OnProcessingCompletion(Action act)
        {
            return ProcessingCompletion.ContinueWith((Task task) =>
            {
                act();
            });
        }
    }
}