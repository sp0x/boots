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
                var parameters = new JObject();
                parameters["model_name"] = model.ModelName;
                parameters["model_id"] = model.Id;
                var collectionsArray = new JArray();
                foreach (var cl in collections)
                {
                    if (string.IsNullOrEmpty(cl.Timestamp))
                    {
                        throw new InvalidOperationException("Collections with timestamp columns are allowed only!");
                    }
                    var collection = new JObject();
                    collection["name"] = cl.Name;
                    collection["key"] = cl.Collection;
                    collection["start"] = cl.Start;
                    collection["end"] = cl.End;
                    collection["index"] = cl.IndexBy;
                    collection["timestamp"] = cl.Timestamp;
                    collectionsArray.Add(collection);
                }
                parameters["collections"] = collectionsArray;
                var relationsArray = new JArray();
                foreach (var relation in relations)
                {
                    relationsArray.Add(new JArray(new object[] { relation.Attribute1, relation.Attribute2 }));
                }
                parameters["relations"] = relationsArray;
                parameters["target"] = targetAttribute;
                qr["params"] = parameters;
                return qr;
            }
        }
    }
}
