using System;
using System.Collections.Generic;
using System.Text;
using Donut;
using Donut.Models;
using Netlyt.Interfaces;
//using Netlyt.Interfaces.Ml;
using Netlyt.Service;
using Netlyt.Service.Integration;

namespace Netlyt.ServiceTests
{
    public class Utils
    {
        public static Model GetModel(ApiAuth apiAuth, string modelName = "Romanian", string integrationName = "Namex")
        {
            var rootIntegration = new DataIntegration(integrationName, true)
            {
                APIKey = apiAuth,
                APIKeyId = apiAuth.Id,
                Name = integrationName,
                DataTimestampColumn = "timestamp",
            };
            rootIntegration.AddField<double>("humidity");
            rootIntegration.AddField<double>("latitude");
            rootIntegration.AddField<double>("longitude");
            rootIntegration.AddField<double>("pm10");
            rootIntegration.AddField<double>("pm25");
            rootIntegration.AddField<double>("pressure");
            rootIntegration.AddField<double>("rssi");
            rootIntegration.AddField<double>("temperature");
            rootIntegration.AddField<DateTime>("timestamp");
            return GetModel(apiAuth, modelName, rootIntegration);
        }
        public static Model GetModel(ApiAuth apiAuth, string modelName, DataIntegration ign)
        {
            var ignName = modelName;//Must match the features
            var model = new Model()
            {
                ModelName = modelName
            };//_db.Models.Include(x=>x.DataIntegrations).FirstOrDefault(x => x.Id == modelId);
            var modelIntegration = new ModelIntegration() { Model = model, Integration = ign };
            model.DataIntegrations.Add(modelIntegration);
            model.User = new User() { UserName = "Testuser" };
            model.TargetAttribute = "pm10";
            return model;
        }
    }
}
