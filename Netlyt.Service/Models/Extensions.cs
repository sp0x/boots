using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Donut.Models;
using Donut.Orion;

namespace Netlyt.Service.Models
{
    public static class Extensions
    {
        public static IEnumerable<FeatureGenerationCollectionOptions> GetFeatureGenerationCollections(this Model model, string targetAttribute)
        {
            var timestampservice = new TimestampService(null);
            foreach (var integration in model.DataIntegrations)
            {
                var ign = integration.Integration;
                var ignTimestampColumn = !string.IsNullOrEmpty(ign.DataTimestampColumn) ? ign.DataTimestampColumn : timestampservice.Discover(ign);
                var fields = ign.Fields;
                InternalEntity intEntity = null;
                if (fields.Any(x => x.Name == targetAttribute))
                {
                    intEntity = new InternalEntity()
                    {
                        Name = targetAttribute
                    };
                }
                var colOptions = new FeatureGenerationCollectionOptions()
                {
                    Collection = ign.Collection,
                    Name = ign.Name,
                    TimestampField = ignTimestampColumn,
                    InternalEntity = intEntity,
                    Integration = ign
                    //Other parameters are ignored for now
                };
                yield return colOptions;
            }
        }
    }
}
