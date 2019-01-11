using System;
using System.Collections.Generic;
using System.Linq;
using Donut.Data;
using Donut.Encoding;
using Donut.Integration;
using Donut.Lex.Expressions;
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

            private static JArray GetFieldsOptions(IIntegration integration, Func<FieldDefinition, bool> fieldFilter = null)
            {
                var fields = new JArray();
                if (fieldFilter == null) fieldFilter = (x) => true;
                foreach (var fld in integration.Fields.Where(fieldFilter))
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
                        var flds = GetEncodedFieldSubfields(integration, fld);
                        foreach (var encFld in flds) fields.Add(encFld);
                    }
                }

                return fields;
            }

            private static IEnumerable<JObject> GetEncodedFieldSubfields(IIntegration cl, FieldDefinition fld)
            {
                var encoding = FieldEncoding.Factory.Create(cl, fld.DataEncoding);
                var encodedFields = encoding.GetFieldNames(fld);
                foreach (var encField in encodedFields)
                {
                    var newField = new JObject
                    {
                        {"name" as string, encField},
                        {"encoding" as string, fld.DataEncoding.ToString().ToLower()},
                        {"id", fld.Id}
                    };
                    yield return newField;
                }
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
                DataIntegration ign,
                IEnumerable<TrainingTask> trainingTasks)
            {
                var rootIntegration = ign;
                var qr = new OrionQuery(OrionOp.Train);
                var parameters = new JObject();
                var models = new JObject();
                var dataOptions = new JObject();
                var autoModel = new JObject();
                trainingTasks = trainingTasks.ToList();
                var flags = GetModelDataFlags(model, null);
                parameters["client"] = model.User.UserName;
                parameters["experiment_name"] = $"Model{model.Id}";
                //Todo update..
                parameters["targets"] = CreateTargetsDef(model.Targets.ToArray());
                parameters["tasks"] = new JArray(trainingTasks.Select(x => x.Id).ToArray());
                parameters["script"] = GetTrainingScript(trainingTasks);
                models["auto"] = autoModel; // GridSearchCV - param_grid
                var sourceCol = ign.GetModelSourceCollection(model);
                dataOptions["db"] = sourceCol;
                dataOptions["start"] = null;
                dataOptions["end"] = null;
                dataOptions["scoring"] = "auto";
                dataOptions["collection"] = flags["collection"];
                //Get fields from the mongo collection, these would be already generated features, so it's safe to use them
                BsonDocument featuresDoc = MongoHelper.GetCollection(sourceCol).AsQueryable().FirstOrDefault();
                var fields = GetFieldsList(featuresDoc, rootIntegration);
                dataOptions["fields"] = fields;
                parameters["models"] = models;
                parameters["options"] = dataOptions;
                parameters["model_id"] = model.Id;

                qr["params"] = parameters;
                return qr;
            }

            private static JArray GetFieldsList(IEnumerable<BsonElement> featureFields, DataIntegration rootIntegration)
            {
                var output = new JArray();
                foreach (var field in featureFields)
                {
                    var jsfld = new JObject();
                    jsfld["name"] = field.Name;
                    var matchingField = rootIntegration.Fields.FirstOrDefault(x => x.Name == field.Name);
                    if (matchingField != null)
                    {
                        jsfld["id"] = matchingField.Id;
                    }
                    var isEncoded = matchingField != null && matchingField.DataEncoding != FieldDataEncoding.None;
                    if (isEncoded)
                    {
                        var encodedFields = GetEncodedFieldSubfields(rootIntegration, matchingField);
                        foreach (var encFld in encodedFields)
                        {
                            encFld["is_key"] = rootIntegration.DataIndexColumn != null &&
                                              !string.IsNullOrEmpty(rootIntegration.DataIndexColumn) &&
                                              encFld["name"].ToString() == rootIntegration.DataIndexColumn;
                            encFld["type"] = "float";
                            //if (encFld.Value.IsDateTime) jsfld["type"] = "datetime";
                            //else if (encFld.Value.IsString) jsfld["type"] = "str";
                            output.Add(encFld);
                        }
                    }
                    else
                    {
                        jsfld["is_key"] = rootIntegration.DataIndexColumn != null &&
                                          !string.IsNullOrEmpty(rootIntegration.DataIndexColumn) &&
                                          field.Name == rootIntegration.DataIndexColumn;
                        jsfld["type"] = "float";
                        if (field.Value.IsDateTime)jsfld["type"] = "datetime";
                        else if (field.Value.IsString)jsfld["type"] = "str";
                        output.Add(jsfld);
                    }

                }
                return output;
            }

            private static JToken GetModelDataFlags(Model model, IEnumerable<BsonElement> fields)
            {
                var output = new JObject();
                var rootIgn = model.GetRootIntegration();
                output["collection"] = new JObject();
                output["collection"]["timestamp"] = rootIgn.DataTimestampColumn;
                output["collection"]["index"] = rootIgn.DataIndexColumn;
                output["collection"]["key"] = model.GetFeaturesCollection();
                output["fields"] = new JArray();
                if (fields != null)
                {
                    var jsFields = GetFieldsList(fields, rootIgn);
                    output["fields"] = jsFields;
                }
                return output;
            }

            private static JToken GetTrainingScript(IEnumerable<TrainingTask> trainingTasks)
            {
                var output = new JObject();
                output["code"] = trainingTasks.Select(x => x.Script).FirstOrDefault()?.PythonScript.ToString();
                return output;
            }

            /// <summary>
            /// Model target constraints to json
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            private static JArray CreateTargetsDef(params ModelTarget[] targets)
            {
                var arrTargets = new JArray();
                foreach (var target in targets)
                {
                    var jsTarget = new JObject();
                    var arrConstraints = new JArray();
                    jsTarget["column"] = target.Column.Name;
                    jsTarget["scoring"] = target.Scoring;
                    jsTarget["task_type"] = target.IsRegression ? "regression" : "classification";
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
            public static OrionQuery CreateProjectGenerationRequest(TrainingTask task)
            {
                var qr = new OrionQuery(OrionOp.GenerateProject);
                var fileParams = new JObject();
                var groupBy = new JObject();
                var script = task.Script;
                //groupBy["hour"] = 0.5;
                //groupBy["day_unix"] = true;
                fileParams["script"] = GetTrainingScript(new []{ task });
                fileParams["client"] = task.User.UserName;
                fileParams["name"] = task.Model.ModelName;
                qr["msg"] = fileParams;
                return qr;
            }

            public static OrionQuery CreateDataDescriptionQuery(DataIntegration ign, IEnumerable<ModelTarget> targets)
            {
                var qr = new OrionQuery(OrionOp.AnalyzeFile);
                var data = new JObject();
                data["src"] = ign.Collection;
                data["src_type"] = "collection";
                data["targets"] = CreateTargetsDef(targets.ToArray());
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
                if (newModel.Targets == null || newModel.Targets.Count==0) return null;
                var qr = new OrionQuery(OrionOp.ParseTargets);
                var fileParams = new JObject();
                var collections = new JArray();
                var rootCollection = newModel.GetFeaturesCollection();
                collections.Add(rootCollection);
                fileParams["targets"] = CreateTargetsDef(newModel.Targets.ToArray());
                fileParams["collections"] = collections;
                qr["params"] = fileParams;
                return qr;
            }

            /// <summary>
            /// Create a script generation query.
            /// </summary>
            /// <param name="model">The model to use.</param>
            /// <param name="ds">The donut script to create a python script for.</param>
            /// <returns></returns>
            public static OrionQuery CreateScriptGenerationQuery(Model model, IDonutScript ds)
            {
                var qr = new OrionQuery(OrionOp.CreateScript);
                var p = new JObject();
                var features = new JArray();
                var ign = model.GetRootIntegration();
                var sourceCol = ign.GetModelSourceCollection(model);
                foreach (var feature in ds.Features)
                {
                    JToken ftrObj = FeatureToJson(feature);
                    features.Add(ftrObj);
                }
                BsonDocument featuresDoc = MongoHelper.GetCollection(sourceCol).AsQueryable().FirstOrDefault();
                var featuresCols = featuresDoc.Elements.Where(x => x.Name != "_id");
                JToken dataFlags = GetModelDataFlags(model, featuresCols);

                var targets = CreateTargetsDef(ds.Targets.ToArray());
                p["features"] = features;
                p["targets"] = targets;
                p["model_id"] = model.Id;
                p["data_flags"] = dataFlags;
                p["use_featuregen"] = model.UseFeatures;
                qr["params"] = p;
                return qr;
            }


            /// <summary>
            /// Converts a feature to { title:.., type: direct|.., key:..}
            /// </summary>
            /// <param name="feature"></param>
            /// <returns></returns>
            private static JToken FeatureToJson(AssignmentExpression feature)
            {
                var output = new JObject();
                var ftrName = feature.Member.ToString();
                var ftrExpression = feature.Value.ToString();
                var featureType = "direct";
                if (feature.Value.GetType() != typeof(NameExpression))
                {
                    throw new NotImplementedException();
                }
                output["type"] = featureType;
                output["title"] = ftrName;
                //Function name
                if (featureType == "direct")
                {
                    output["key"] = ftrExpression;
                }
                else
                {
                    throw new NotImplementedException();
                }
                return output;
            }
        }
    }
}
