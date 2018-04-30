using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Blocks;

namespace Donut.Blocks
{
    /// <summary>
    /// An entity grouping block
    /// </summary>
    public class GroupingBlock<T> : BaseFlowBlock<T, T>
        where T : class, IIntegratedDocument
    {
        #region "Variables"
        private Func<T, object> _groupBySelector;
        /// <summary>
        /// 
        /// </summary>
        private Func<T, BsonDocument, object> _accumulator;
        /// <summary>
        /// 
        /// </summary>
        private Action<T> _inputProjection;
        #endregion

        #region "Props"
//        public event EventHandler<EventArgs> GroupingComplete;

        public ConcurrentDictionary<object, T> EntityDictionary { get; private set; } 
        
        //public BsonArray Purchases { get; set; }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="selector"></param>
        /// <param name="inputProjection">Projection to perform on the input</param>
        /// <param name="accumulator">Accumulate input data to the resolved element</param>
        public GroupingBlock(ApiAuth apiKey, Func<T, object> selector,
            Action<T> inputProjection,
            Func<T, BsonDocument, object> accumulator)
            : base(capacity: 1000, procType: BlockType.Action)
        {
            base.AppId = apiKey.AppId;
            _groupBySelector = selector;
            this._accumulator = accumulator;
            this._inputProjection = inputProjection;
            //base.Completed += OnReadingCompleted;
            EntityDictionary = new ConcurrentDictionary<object, T>();
            //PageStats = new CrossPageStats();
        } 
         

        protected override IEnumerable<T> GetCollectedItems()
        {
            return EntityDictionary.Values;
        } 
        protected override T OnBlockReceived(T intDoc)
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
                        docClone = intDoc.Clone() as T;
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
//            if (!Helper.Stats.ContainsPage(pageHost))
//            {
//                Helper.Stats.AddPage(pageSelector, new PageStats()
//                {
//                    Page = page
//                });
//                isNewPage = true;
//            }
            if (isNewUser)
            {
                //this.PageStats[pageSelector].UsersVisitedTotal++;
            }
            if (!isNewPage)
            {
                //var duration = this.PageStats[pageSelector].VisitStarted;
            }
            //Helper.Stats[pageSelector].PageVisitsTotal++;
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