using Netlyt.Interfaces;

namespace Donut
{
    public class DonutFeatureDefinition : IDonutFeatureDefinition
    {
        private string _content;

        public DonutFeatureDefinition(string content)
        {
            _content = content;
        }

        public string GetValue()
        {
            return _content;
        }
    }
}