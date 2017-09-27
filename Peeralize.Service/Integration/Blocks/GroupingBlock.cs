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
    public class GroupingBlock : IntegrationBlock
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
        public event EventHandler<EventArgs> GroupingComplete;

        public ConcurrentDictionary<object, IntegratedDocument> EntityDictionary { get; private set; }
        public CrossPageStats PageStats { get; set; }

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
            base.Completed += OnReadingCompleted;
            EntityDictionary = new ConcurrentDictionary<object, IntegratedDocument>();
            PageStats = new CrossPageStats();
        } 

        private void OnReadingCompleted()
        {
            //We have red all the available records, our grouping should be complete
            GroupingComplete?.Invoke(this, EventArgs.Empty);
        }

        protected override IEnumerable<IntegratedDocument> GetCollectedItems()
        {
            return EntityDictionary.Values;
        }

        public override void Complete()
        {
//            foreach (var group in EntityDictionary)
//            {
//                BsonArray bsonValue = (BsonArray)EntityDictionary[@group.Key].Document["events"];
//                EntityDictionary[group.Key].Document["events"] = new BsonArray(
//                    bsonValue.OrderBy(x=>DateTime.Parse(x["ondate"].ToString())));
//            }
            base.Complete();
        }

        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            //Get key
            var key = _groupBySelector==null ? null : _groupBySelector(intDoc);
            var intDocDocument = intDoc.GetDocument();
//            var page = intDocDocument["value"];
//            if (page.ToString().IsNumeric())
//            {
//                return intDoc;
//            }
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
            
            //RecordPageStats(key.ToString(), intDocDocument, isNewUser);
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
        private void RecordPageStats(string userId, BsonDocument eventData, bool isNewUser)
        {
            var page = eventData["value"].ToString();
            var pageHost = page.ToHostname();
            var pageSelector = pageHost;
            var isNewPage = false;
            if (!PageStats.ContainsPage(pageHost))
            {
                PageStats.AddPage(pageSelector, new PageStats()
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
            this.PageStats[pageSelector].PageVisitsTotal++;
        }
        

//        public static TimeSpan GetPageChangeTimespan(DateTime startingTime, BsonArray events, Func<string, bool> toPageFilter, int offset = 0)
//        {
//            for (var i = offset; i < events.Count; i++)
//            {
//                var eventData = events[i];
//                var eventPage = eventData["value"].ToString();
//
//                if (toPageFilter(eventPage))
//                {
//                    var pageVisitTime = DateTime.Parse(eventData["ondate"].ToString());
//                    return pageVisitTime - startingTime;
//                }
//            }
//            return TimeSpan.Zero;
//        }

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