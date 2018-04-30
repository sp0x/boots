namespace Donut
{
    public class DatasetMember
    {
        public string Name { get; private set; }
        public DataIntegration Integration { get; private set; }
        public DatasetMember(DataIntegration integration)
        {
            this.Name = integration.Name;
            this.Integration = integration;
        }

        public string GetPropertyName()
        {
            var sName = Name.Replace(' ', '_').Replace('.', '_').Replace('-', '_').Replace(';', '_');
            return "Ds" + sName;
        }
    }
}