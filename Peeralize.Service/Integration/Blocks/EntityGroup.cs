using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using nvoid.extensions;
using System.Linq;

namespace Peeralize.Service.Integration.Blocks
{
    /// <summary>
    /// 
    /// </summary>
    public class EntityGroup : IntegrationBlock
    {
        private Func<IntegratedDocument, object> GroupBySelector;
        private Func<IntegratedDocument, IntegratedDocument, object> ModifierAction;
        private Action<IntegratedDocument> OnUserCreatedFilter; 
        public event EventHandler<EventArgs> GroupingComplete;

        public Dictionary<object, IntegratedDocument> EntityDictionary { get; private set; }
        public CrossPageStats PageStats { get; set; }
        //public BsonArray Purchases { get; set; }



        public EntityGroup(string userId, Func<IntegratedDocument, object> selector,
            Action<IntegratedDocument> filterUserCreation,
            Func<IntegratedDocument, IntegratedDocument, object> accumulator) : base()
        {
            base.UserId = userId;
            GroupBySelector = selector;
            ModifierAction = accumulator;
            OnUserCreatedFilter = filterUserCreation;
            base.Completed += OnReadingCompleted;
            EntityDictionary = new Dictionary<object, IntegratedDocument>();
            PageStats = new CrossPageStats();
        }

        public IIntegrationDestination ContinueWith(Action<EntityGroup> action)
        {
            var completion = GetActionBlock().Completion;
            completion.ContinueWith(xTask =>
            {
                action(this);
            });
            return this;
        }

        private void OnReadingCompleted()
        {
            //We have red all the available records, our grouping should be complete
            GroupingComplete?.Invoke(this, EventArgs.Empty);
        }


        public override void Close()
        {
            foreach (var group in EntityDictionary)
            {
                BsonArray bsonValue = (BsonArray)EntityDictionary[@group.Key].Document["events"];
                EntityDictionary[group.Key].Document["events"] = new BsonArray(
                    bsonValue.OrderBy(x=>DateTime.Parse(x["ondate"].ToString())));
            }
            base.Close();
        }


        protected override IntegratedDocument OnBlockReceived(IntegratedDocument intDoc)
        {
            var key = GroupBySelector(intDoc);
            var page = intDoc.Document["value"];
            if (page.ToString().IsNumeric())
            {
                return intDoc;
            }

            var isNewUser = false;
            if (!EntityDictionary.ContainsKey(key))
            {
                var docClone = intDoc;
                //Ignore non valid values
                if (OnUserCreatedFilter != null)
                {
                    docClone = intDoc.Clone();
                    OnUserCreatedFilter(docClone); 
                }
                EntityDictionary[key] = docClone;
                isNewUser = true;
            }
            RecordPageStats(key.ToString(), intDoc.Document, isNewUser);
            var newElement = ModifierAction(EntityDictionary[key], intDoc);
            return EntityDictionary[key];
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
         
    }
}