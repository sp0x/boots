namespace Donut
{
    public class DatasetMember
    {
        public string Name { get; private set; }
        public Data.DataIntegration Integration { get; private set; }
        public DatasetMember(Data.DataIntegration integration)
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