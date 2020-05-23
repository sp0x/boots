using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Donut.Data;
using Donut.Models;
using Donut.Orion;

namespace Netlyt.Service.Models
{
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="targetAttributes"></param>
        /// <returns></returns>
        public static IEnumerable<FeatureGenerationCollectionOptions> GetFeatureGenerationCollections(this Model model, ModelTarget targetAttributes)
        {
            var timestampservice = new TimestampService(null);
            foreach (var integration in model.DataIntegrations)
            {
                var ign = integration.Integration;
                var ignTimestampColumn = !string.IsNullOrEmpty(ign.DataTimestampColumn) ? ign.DataTimestampColumn : timestampservice.Discover(ign);
                var colOptions = new FeatureGenerationCollectionOptions()
                {
                    Collection = ign.Collection,
                    Name = ign.Name,
                    TimestampField = ignTimestampColumn,
                    //InternalEntity = intEntity,
                    Integration = ign
                    //Other parameters are ignored for now
                };
                yield return colOptions;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="integrations"></param>
        /// <param name="targetAttributes"></param>
        /// <returns></returns>
        public static IEnumerable<FeatureGenerationCollectionOptions> GetFeatureGenerationCollections(
            this IEnumerable<DataIntegration> integrations,
            params ModelTarget[] targetAttributes)
        {
            var timestampservice = new TimestampService(null);
            foreach (var integration in integrations)
            {
                var ign = integration;
                var ignTimestampColumn = !string.IsNullOrEmpty(ign.DataTimestampColumn) ? ign.DataTimestampColumn : timestampservice.Discover(ign);
                var colOptions = new FeatureGenerationCollectionOptions()
                {
                    Collection = ign.Collection,
                    Name = ign.Name,
                    TimestampField = ignTimestampColumn,
                    //InternalEntity = intEntity,
                    Integration = ign
                    //Other parameters are ignored for now
                };
                yield return colOptions;
            }
        }
    }
}
