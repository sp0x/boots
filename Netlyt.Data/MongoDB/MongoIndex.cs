using System;
using System.Collections.Generic;
using System.Diagnostics;
using Donut.Data;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netlyt.Data.MongoDB
{
      //
    public class MongoIndex<TRecord>
    {
        public IndexKeysDefinition<TRecord> Keys { get; set; }
        public CreateIndexOptions Options { get; set; }

        public MongoIndex(IndexKeysDefinition<TRecord> keys, bool unique = false)
        {
            this.Keys = keys;
            Options = new CreateIndexOptions();
            if (unique)
                Options.Unique = true;
            Options.Name = GetName(); 
        }

        public WriteConcernResult Apply(ref IMongoCollection<TRecord> records)
        {
            if (!records.IndexExists(Keys))
            {
                records.Indexes.CreateOne(Keys, Options);
            }
            return null;
        }

        public string GetName()
        { 
            var nameList = new List<String>();
            JObject keyItems = (JObject) JsonConvert.DeserializeObject(Keys.ToString());

            foreach (var key in keyItems.Properties())
            { 
                var ixKeyName = key.Name;
                var name = ixKeyName;
                var nLength = name.Length;
                if (name.Length > 5)
                {
                    name = name.Remove(4) + "`";
                }
                nameList.Add(name);
            }
            return string.Join("_", nameList);
        }
        public bool TryApply(IMongoCollection<TRecord> records) 
        {
            try
            {
                //var index = records.Indexes.List().ToEnumerable().Where(x=> x["keys"].Equals(BsonSerializer.Serialize(Keys)));
                if (!records.IndexExists(Keys))
                { 
                    records.Indexes.CreateOne(Keys, Options);
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);

                //                do
                //                {
                //                    try
                //                    {
                //                        if (!records.IndexExists(Keys))
                //                        {
                //                            return !records.CreateIndex(Keys, Options).HasLastErrorMessage;
                //                        }
                //                    }
                //                    catch (Exception ex2)
                //                    {
                //                        ex2 = ex2;
                //                    }
                //                } while (true); 
            }
            return false;
        }
    }
//    public class MongoIndex<TRecord> : MongoIndex where TRecord : IdAble
//    {
//        public IndexKeysDefinitionBuilder<TRecord> Keys
//        {
//            get { return null; } // (IndexKeysDefinitionBuilder<TRecord>)base.Keys
//            set { base.Keys = null; }
//        }
//        public CreateIndexOptions<TRecord> Options
//        {
//            get { return (CreateIndexOptions<TRecord>)base.Options; }
//            set { base.Options = value; }
//        }
//        public MongoIndex(IndexKeysDefinitionBuilder<TRecord> keys, bool unique = false) : base(null, unique)
//        {
//            unique = unique;
//        }
//    }
}