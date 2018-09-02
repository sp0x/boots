using System;
using System.Collections.Generic;
using Donut.Data;
using Donut.Source;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;

namespace Donut.Integration
{
    /// <summary>
    /// Describes a data type that will be used in an integration
    /// </summary>
    public interface IIntegration
    {
        long Id { get; set; }
        DateTime CreatedOn { get; set; }
        string Name { get; set; }
        int DataEncoding { get; set; }
        User Owner { get; set; }
        ApiAuth APIKey { get; set; }
        string Collection { get; set; }
        string FeaturesCollection { get; set; }
        string DataTimestampColumn { get; set; }
        string DataIndexColumn { get; set; }
        string FeatureScript { get; set; }
        string CreatedOnNodeToken { get; set; }
        /// <summary>
        /// The type of origin of this type
        /// </summary>
        string DataFormatType { get; }
        ICollection<FieldDefinition> Fields { get; }

        ICollection<IntegrationExtra> Extras { get; }
        ICollection<Permission> Permissions { get; set; }

        IIntegratedDocument CreateDocument<T>(T data);
        FieldDefinition GetField(string name);
        /// <summary>
        /// Gets the keys that are used to aggregate the data in this collection.
        /// </summary>
        /// <returns></returns>
        //IEnumerable<AggregateKey> GetAggregateKeys();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        string GetReducedCollectionName();
        
    }
}