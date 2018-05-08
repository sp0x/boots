using Netlyt.Interfaces;

namespace Donut
{
    public class InternalDonutFunctionProxy : DonutFunction
    {
        public InternalDonutFunctionProxy(string nm, string content) : base(nm)
        {
            base.IsAggregate = false;
            this.Content = new DonutFeatureDefinition(content);
            this.GroupValue = content;
            base.Type = DonutFunctionType.GroupField;
        }
    }
}