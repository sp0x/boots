using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Encoding;
using Donut.Models;
using Netlyt.Interfaces;
using Newtonsoft.Json.Linq;

namespace Donut.Orion
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
                var qr = new global::Donut.Orion.OrionQuery(global::Donut.Orion.OrionOp.GenerateFeatures);
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
                    var binFields =
                        cl.Integration.Fields.Where(x => x.DataEncoding == FieldDataEncoding.BinaryIntId);
                    collection["fields"] = new JArray();
                    foreach (var fld in cl.Integration.Fields)
                    {
                        var isEncoded = fld.DataEncoding != FieldDataEncoding.None;
                        if (!isEncoded)
                        {
                            var jField = new JObject();
                            jField["name"] = fld.Name;
                            (collection["fields"] as JArray).Add(jField);
                        }
                        else
                        {
                            var encoding = FieldEncoding.Factory.Create(cl.Integration, fld.DataEncoding);
                            var encodedFields = encoding.GetFieldNames(fld);
                            foreach (var encField in encodedFields)
                            {
                                var newField = new JObject
                                {
                                    {"name" as string, encField},
                                    {"encoding" as string, fld.DataEncoding.ToString().ToLower()}
                                };
                                (collection["fields"] as JArray).Add(newField);
                            }
                        }
                    }
                    if (cl.InternalEntity != null)
                    {
                        var intEntity = new JObject();
                        intEntity["collection"] = cl.Collection;
                        intEntity["name"] = cl.InternalEntity.Name;
                        intEntity["index"] = $"{cl.InternalEntity.Name}_id";
                        intEntity["fields"] = new JArray(new string[] { "_id", cl.InternalEntity.Name });
                        collection["internal_entity_keys"] = new JArray(new string[] { intEntity["index"].ToString() });
                        internalEntities.Add(intEntity);
                    }
                    collectionsArray.Add(collection);
                }
                parameters["collections"] = collectionsArray;
                var relationsArray = new JArray();
                if (relations != null)
                {
                    foreach (var relation in relations)
                    {
                        relationsArray.Add(new JArray(new object[] { relation.Attribute1, relation.Attribute2 }));
                    }
                }
                parameters["relations"] = relationsArray;
                parameters["target"] = targetAttribute;
                parameters["internal_entities"] = internalEntities;
                qr["params"] = parameters;
                return qr;
            }

            public static string GetDefaultScoring()
            {
                return "r2";
            }

            public static OrionQuery CreateTrainQuery(Model model, DataIntegration ign)
            {
                var rootIntegration = ign;
                var qr = new global::Donut.Orion.OrionQuery(OrionOp.Train);
                var parameters = new JObject();
                var models = new JObject();
                var dataOptions = new JObject();
                var autoModel = new JObject();
                var scoring = GetDefaultScoring();
                parameters["client"] = model.User.UserName;
                parameters["target"] = model.TargetAttribute;
                models["auto"] = autoModel; // GridSearchCV - param_grid

                dataOptions["db"] = rootIntegration.FeaturesCollection;
                dataOptions["start"] = null; //DateTime.MinValue;
                dataOptions["end"] = null;// DateTime.MaxValue;
                dataOptions["scoring"] = scoring;


                parameters["models"] = models;
                parameters["options"] = dataOptions;
                parameters["model_id"] = model.Id;

                qr["params"] = parameters;
                return qr;
            }
        }
    }
}
