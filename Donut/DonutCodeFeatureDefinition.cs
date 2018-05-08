using Netlyt.Interfaces;

namespace Donut
{
    public class DonutCodeFeatureDefinition : IDonutCodeFeatureDefinition
    {

        /// <summary>
        /// The script that gathers information from an integration's documents before extracting a feature.
        /// </summary>
        public string PrepareScript { get; set; }

        public string ExtractionScript { get; set; }
        public string GetValue()
        {
            return PrepareScript;
        }
    }
}