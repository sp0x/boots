using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Data;
using Donut.Encoding;
using Donut.Integration;
using Donut.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Newtonsoft.Json;
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
            public static OrionQuery CreateFeatureDefinitionGenerationQuery(Model model,
                IEnumerable<FeatureGenerationCollectionOptions> collections,
                IEnumerable<FeatureGenerationRelation> relations,
                ModelTargets targetAttribute)
            {
                var qr = new global::Donut.Orion.OrionQuery(global::Donut.Orion.OrionOp.GenerateFeatures);
                var parameters = new JObject();
                parameters["model_name"] = model.ModelName;
                parameters["model_id"] = model.Id;
                parameters["client"] = model.User.UserName;
                parameters["verbose"] = true;
                parameters["export_features"] = true;
                var collectionsArray = new JArray();
                var internalEntities = new JArray();
                foreach (var cl in collections)
                {
                    if (string.IsNullOrEmpty(cl.TimestampField))
                    {
                        //throw new InvalidOperationException("Collections with timestamp columns are allowed only!");
                        Console.WriteLine("Warn: Collections with timestamp columns are allowed only!");
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
                    collection["fields"] = GetFieldsOptions(cl.Integration);
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
                //Use first collection only
                parameters["collection"] = collectionsArray[0];
                var relationsArray = new JArray();
                if (relations != null)
                {
                    foreach (var relation in relations)
                    {
                        relationsArray.Add(new JArray(new object[] { relation.Attribute1, relation.Attribute2 }));
                    }
                }
                //Targets
                var targetsObj = new JObject();


                parameters["relations"] = relationsArray;
                parameters["targets"] = targetsObj;
                parameters["internal_entities"] = internalEntities;
                qr["params"] = parameters;
                return qr;
            }

            private static JArray GetFieldsOptions(IIntegration cl)
            {
                var fields = new JArray();
                foreach (var fld in cl.Fields)
                {
                    var isEncoded = fld.DataEncoding != FieldDataEncoding.None;
                    if (!isEncoded)
                    {
                        var jField = new JObject();
                        jField["name"] = fld.Name;
                        (fields).Add(jField);
                    }
                    else
                    {
                        var encoding = FieldEncoding.Factory.Create(cl, fld.DataEncoding);
                        var encodedFields = encoding.GetFieldNames(fld);
                        foreach (var encField in encodedFields)
                        {
                            var newField = new JObject
                            {
                                {"name" as string, encField},
                                {"encoding" as string, fld.DataEncoding.ToString().ToLower()}
                            };
                            (fields).Add(newField);
                        }
                    }
                }

                return fields;
            }

            public static string GetDefaultScoring()
            {
                return "r2";
            }

            public static OrionQuery CreateTrainQuery(Model model, Data.DataIntegration ign)
            {
                var rootIntegration = ign;
                var qr = new OrionQuery(OrionOp.Train);
                var parameters = new JObject();
                var models = new JObject();
                var dataOptions = new JObject();
                var autoModel = new JObject();
                var scoring = GetDefaultScoring();
                parameters["client"] = model.User.UserName;
                parameters["targets"] = CreateTargetsDef(model.Targets) as JObject;
                models["auto"] = autoModel; // GridSearchCV - param_grid
                var sourceCol = ign.GetModelSourceCollection(model);
                dataOptions["db"] = sourceCol;
                dataOptions["start"] = null; //DateTime.MinValue;
                dataOptions["end"] = null;// DateTime.MaxValue;
                dataOptions["scoring"] = scoring;
                //Get fields from the mongo collection
                BsonDocument featuresDoc = MongoHelper.GetCollection(sourceCol).AsQueryable().FirstOrDefault();
                var fields = new JArray();
                foreach (var field in featuresDoc.Elements.Where(x=>x.Name!="_id"))
                {
                    var jsfld = new JObject();
                    jsfld["name"] = field.Name;
                    fields.Add(jsfld);
                }
                dataOptions["fields"] = fields;

                parameters["models"] = models;
                parameters["options"] = dataOptions;
                parameters["model_id"] = model.Id;

                qr["params"] = parameters;
                return qr;
            }

            /// <summary>
            /// Model target constraints to json
            /// </summary>
            /// <param name="modelTargets"></param>
            /// <returns></returns>
            private static JObject CreateTargetsDef(ModelTargets modelTargets)
            {
                var output = new JObject();
                var arrColumns = new JArray();
                var arrConstraints = new JArray();
                foreach (var col in modelTargets.Columns)
                {
                    arrColumns.Add(col.Name);
                }
                foreach (var constraint in modelTargets.Constraints)
                {
                    var jsConstraint = new JObject();
                    jsConstraint["type"] = constraint.Type.ToString().ToLower();
                    jsConstraint["key"] = constraint.Key;
                    if (constraint.After != null)
                    {
                        jsConstraint["after"] = new JObject();
                        jsConstraint["after"]["hours"] = constraint.After.Hours;
                        jsConstraint["after"]["hours"] = constraint.After.Seconds;
                        jsConstraint["after"]["hours"] = constraint.After.Days;
                    }
                    if (constraint.Before != null)
                    {
                        jsConstraint["before"] = new JObject();
                        jsConstraint["before"]["hours"] = constraint.Before.Hours;
                        jsConstraint["before"]["hours"] = constraint.Before.Seconds;
                        jsConstraint["before"]["hours"] = constraint.Before.Days;
                    }
                    arrConstraints.Add(jsConstraint);
                }
                output["columns"] = arrColumns;
                output["constraints"] = arrConstraints;
                return output;
            }

            public static OrionQuery CreatePredictionQuery(Model model, DataIntegration getRootIntegration)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="url"></param>
            /// <param name="formatting"></param>
            /// <returns></returns>
            public static OrionQuery CreateFileAnalyzeRequest(string url, JObject formatting)
            {
                var qr = new OrionQuery(OrionOp.AnalyzeFile);
                var fileParams = new JObject();
                fileParams["file"] = url;
                fileParams["formatting"] = formatting;
                qr["msg"] = fileParams;
                return qr;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="url"></param>
            /// <param name="formatting"></param>
            /// <returns></returns>
            public static OrionQuery CreateProjectGenerationRequest(string url, JObject formatting)
            {
                var qr = new OrionQuery(OrionOp.GenerateProject);
                var fileParams = new JObject();
                var groupBy = new JObject();
                groupBy["hour"] = 0.5;
                groupBy["day_unix"] = true;

                fileParams["target"] = new JObject();
                fileParams["cleanup"] = new JObject();
                fileParams["feature_settings"] = new JObject();
                fileParams["aggregation"] = new JObject();
                fileParams["target"]["constraints"] = null;
                fileParams["target"]["columns"] = null;
                fileParams["cleanup"]["pre_features"] = null;
                fileParams["cleanup"]["train"] = null;
                fileParams["field_formatting"] = null;
                fileParams["features_source_file"] = url;
                fileParams["feature_settings"]["use_features"] = false;
                fileParams["aggregation"]["group_by"] = groupBy;
                qr["msg"] = fileParams;
                return qr;
            }
        }
    }
}
