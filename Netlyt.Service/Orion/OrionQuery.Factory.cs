using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Service.Integration;
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
                var internalEntities = new JArray();
                foreach (var cl in collections)
                {
                    if (string.IsNullOrEmpty(cl.TimestampField))
                    {
                        throw new InvalidOperationException("Collections with timestamp columns are allowed only!");
                    }
                    var collection = new JObject();
                    collection["name"] = cl.Name;
                    collection["key"] = cl.Collection;
                    collection["start"] = cl.Start;
                    collection["end"] = cl.End;
                    collection["index"] = cl.IndexBy;
                    collection["timestamp"] = cl.TimestampField;
                    collection["internal_entity_keys"] = null;
                    if (cl.InternalEntity != null)
                    {
                        var intEntity = new JObject();
                        intEntity["collection"] = cl.Collection;
                        intEntity["name"] = cl.InternalEntity.Name;
                        intEntity["index"] = $"{cl.InternalEntity.Name}_id";
                        intEntity["fields"] = new JArray(new string[] { "_id", cl.InternalEntity.Name });
                        collection["internal_entity_keys"] = new JArray(new string[]{ intEntity["index"].ToString() });
                        internalEntities.Add(intEntity);
                    }
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
                parameters["internal_entities"] = internalEntities; 
                qr["params"] = parameters;
                return qr;
            }

            public static OrionQuery CreateTrainQuery(Model model, DataIntegration ign)
            {
                var rootIntegration = ign;
                var qr = new OrionQuery(OrionOp.Train);
                var parameters = new JObject();
                var models = new JObject();
                var dataOptions = new JObject();
                var autoModel = new JObject();
                parameters["client"] = model.User.UserName;
                parameters["target"] = model.TargetAttribute;
                models["auto"] = autoModel; // GridSearchCV - param_grid
                
                dataOptions["db"] = rootIntegration.FeaturesCollection;
                dataOptions["start"] = null; //DateTime.MinValue;
                dataOptions["end"] = null;// DateTime.MaxValue;
                dataOptions["scoring"] = "";


                parameters["models"] = models;
                parameters["options"] = dataOptions;
                parameters["model_id"] = model.Id;

                qr["params"] = parameters;  
                return qr;
            }
        }
    }
}
