using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Data;
using Donut.Encoding;
using Donut.Integration;
using Donut.Models;
using Donut.Source;
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
            /// <param name="targetAttributes">A collection's attribute to target. Example `Users.someField` </param>
            /// <returns></returns>
            public static OrionQuery CreateFeatureDefinitionGenerationQuery(Model model,
                IEnumerable<FeatureGenerationCollectionOptions> collections,
                IEnumerable<FeatureGenerationRelation> relations,
                IEnumerable<FieldDefinition> selectedFields,
                params ModelTarget[] targetAttributes)
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
                Func<FieldDefinition, bool> fieldFilter = (field) =>
                    selectedFields == null || selectedFields.Any(x => x.Id == field.Id);
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
                    //var binFields =
                    //    cl.Integration.Fields.Where(x => x.DataEncoding == FieldDataEncoding.BinaryIntId);
                    collection["fields"] = GetFieldsOptions(cl.Integration, fieldFilter);

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
                parameters["relations"] = relationsArray;
                parameters["targets"] = CreateTargetsDef(targetAttributes);
                parameters["internal_entities"] = internalEntities;
                qr["params"] = parameters;
                return qr;
            }

            private static JArray GetFieldsOptions(IIntegration cl, Func<FieldDefinition, bool> fieldFilter = null)
            {
                var fields = new JArray();
                if (fieldFilter == null) fieldFilter = (x) => true;
                foreach (var fld in cl.Fields.Where(fieldFilter))
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

            /// <summary>
            /// TODO: Simplify
            /// </summary>
            /// <param name="model"></param>
            /// <param name="ign"></param>
            /// <param name="trainingTasks"></param>
            /// <returns></returns>
            public static OrionQuery CreateTrainQuery(
                Model model,
                Data.DataIntegration ign,
                IEnumerable<TrainingTask> trainingTasks)
            {
                var rootIntegration = ign;
                var qr = new OrionQuery(OrionOp.Train);
                var parameters = new JObject();
                var models = new JObject();
                var dataOptions = new JObject();
                var autoModel = new JObject();
                var scoring = GetDefaultScoring();
                parameters["client"] = model.User.UserName;
                //Todo update..
                parameters["targets"] = CreateTargetsDef(model.Targets);
                parameters["tasks"] = new JArray(trainingTasks.Select(x => x.Id).ToArray());
                models["auto"] = autoModel; // GridSearchCV - param_grid
                var sourceCol = ign.GetModelSourceCollection(model);
                dataOptions["db"] = sourceCol;
                dataOptions["start"] = null;
                dataOptions["end"] = null;
                dataOptions["scoring"] = "auto"; 
                //Get fields from the mongo collection, these would be already generated features, so it's safe to use them
                BsonDocument featuresDoc = MongoHelper.GetCollection(sourceCol).AsQueryable().FirstOrDefault();
                var fields = new JArray();
                foreach (var field in featuresDoc.Elements.Where(x=>x.Name!="_id"))
                {
                    var jsfld = new JObject();
                    jsfld["name"] = field.Name;
                    jsfld["is_key"] = rootIntegration.DataIndexColumn != null &&
                                      !string.IsNullOrEmpty(rootIntegration.DataIndexColumn) &&
                                      field.Name == rootIntegration.DataIndexColumn;
                    jsfld["type"] = "float";
                    if (field.Value.IsDateTime)
                    {
                        jsfld["type"] = "datetime";
                    }else if (field.Value.IsString)
                    {
                        jsfld["type"] = "str";
                    }
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
            /// <param name="target"></param>
            /// <returns></returns>
            private static JArray CreateTargetsDef(IEnumerable<ModelTarget> targets)
            {
                var arrTargets = new JArray();
                foreach (var target in targets)
                {
                    var jsTarget = new JObject();
                    var arrConstraints = new JArray();
                    jsTarget["column"] = target.Column.Name;
                    foreach (var constraint in target.Constraints)
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
                    jsTarget["constraints"] = arrConstraints;
                    arrTargets.Add(jsTarget);
                }
                return arrTargets;
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

            public static OrionQuery CreateDataDescriptionQuery(DataIntegration ign, IEnumerable<ModelTarget> targets)
            {
                var qr = new OrionQuery(OrionOp.AnalyzeFile);
                var data = new JObject();
                data["src"] = ign.Collection;
                data["src_type"] = "collection";
                data["targets"] = CreateTargetsDef(targets);
                data["formatting"] = new JObject();
                qr["params"] = data;
                return qr;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="url"></param>
            /// <param name="formatting"></param>
            /// <returns></returns>
            public static OrionQuery CreateDataDescriptionQuery(string url, JObject formatting)
            {
                var qr = new OrionQuery(OrionOp.AnalyzeFile);
                var fileParams = new JObject();
                fileParams["src"] = url;
                fileParams["src_type"] = "collection";
                fileParams["formatting"] = formatting;
                qr["params"] = fileParams;
                return qr;
            }

            public static OrionQuery CreateTargetParsingQuery(Model newModel)
            {
                var qr = new OrionQuery(OrionOp.ParseTargets);
                var fileParams = new JObject();
                var collections = new JArray();
                var rootCollection = newModel.GetFeaturesCollection();
                collections.Add(rootCollection);
                fileParams["targets"] = CreateTargetsDef(newModel.Targets);
                fileParams["collections"] = collections;
                qr["params"] = fileParams;
                return qr;
            }
        }
    }
}
