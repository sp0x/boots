using System;

namespace Netlyt.Data.ViewModels
{
    public class AccessLogViewModel
    {
        public string User { get; set; }
        public string CreatedOn { get; set; }
        public string Event { get; set; }
    }
    //Deprecated..
    public class DataIntegrationViewModel
    {
        public long Id { get; set; }
        public string FeatureScript { get; set; }
        public string Name { get; set; }
        public int DataEncoding { get; set; }
        public DateTime CreatedOn { get; set; }
        public long? PublicKeyId { get; set; }
        /// <summary>
        /// the type of the data e.g stream or file
        /// </summary>
        public string DataFormatType { get; set; }
        /// <summary>
        /// The source from which the integration is registered to receive data.
        /// Could be url or just a hint.
        /// </summary>
        public string Source { get; set; }
        public string Collection { get; set; }

        public string FeaturesCollection { get; set; }
        public ApiAuthViewModel APIKey { get; set; }
    }
}