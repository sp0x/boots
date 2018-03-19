using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Service.Ml;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Orion
{
    public partial class OrionQuery
    {
        public class Factory
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="model"></param>
            /// <param name="collections"></param>
            /// <param name="relations"></param>
            /// <param name="targetAttribute">A collection's attribute to target. Example `Users.someField` </param>
            /// <returns></returns>
            public static OrionQuery CreateFeatureGenerationQuery(Model model,
                IEnumerable<FeatureGenerationCollectionOptions> collections,
                IEnumerable<FeatureGenerationRelation> relations,
                string targetAttribute)
            {
                var qr = new OrionQuery(OrionOp.GenerateFeatures);
                qr["model_name"] = model.ModelName;
                var collectionsArray = new JArray();
                foreach (var cl in collections)
                {
                    var collection = new JObject();
                    collection["name"] = cl.Name;
                    collection["key"] = cl.Collection; ;
                    collection["start"] = cl.Start;
                    collection["end"] = cl.End;
                    collection["index"] = cl.IndexBy;
                    collectionsArray.Add(collection);
                }
                qr["collections"] = collectionsArray;
                var relationsArray = new JArray();
                foreach (var relation in relations)
                {
                    relationsArray.Add(new JArray(new object[] { relation.Attribute1, relation.Attribute2 }));
                }
                qr["relations"] = relationsArray;
                qr["target"] = targetAttribute;
                return qr;
            }
        }
    }
}
