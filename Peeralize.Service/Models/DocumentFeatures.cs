using System;
using System.Collections.Generic;
using System.Text;
using Peeralize.Service.Integration;

namespace Peeralize.Service.Models
{
    /// <summary>
    /// Describes a document and it's features as key-value(double) types
    /// </summary>
    public class DocumentFeatures
    {
        /// <summary>
        /// The features of the document.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> Features { get; set; }
        /// <summary>
        /// The document to which the features are related
        /// </summary>
        public IntegratedDocument Document { get; set; }

        public DocumentFeatures(IntegratedDocument doc, IEnumerable<KeyValuePair<string, object>> features)
        {
            Document = doc;
            this.Features = features;
        }
    }
}
