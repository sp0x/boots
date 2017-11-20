using System.Collections.Generic;

namespace Netlyt.Service.Models
{

    public interface IFeaturesWrapper
    {
        IEnumerable<KeyValuePair<string, object>> Features { get; set; }
    }
    public interface IDocumentFeatures<T>
        : IFeaturesWrapper
    {
        T Document { get; set; }
    }

    public class FeaturesWrapper<T> : IDocumentFeatures<T>
    {

        /// <summary>
        /// The features of the document.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> Features { get; set; }

        /// <summary>
        /// The document to which the features are related
        /// </summary>
        public T Document { get; set; }
        public FeaturesWrapper() { }
        protected FeaturesWrapper(T doc, IEnumerable<KeyValuePair<string, object>> features)
        {
            this.Document = doc;
            this.Features = features;

        }
    }
}