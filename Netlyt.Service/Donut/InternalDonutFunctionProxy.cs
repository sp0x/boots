namespace Netlyt.Service.Donut
{
    public class InternalDonutFunctionProxy : DonutFunction
    {
        public InternalDonutFunctionProxy(string nm, string content) : base(nm)
        {
            base.IsAggregate = false;
            this.Content = content;
            this.GroupValue = content;
            base.Type = DonutFunctionType.Group;
        }
    }
}