namespace Netlyt.Data.ViewModels
{
    public class ModelCreationViewModel
    {
        public string Name { get; set; }
        public string DataSource { get; set; }
        public string Callback { get; set; }
        public bool GenerateFeatures { get; set; }
        public string[][] Relations { get; set; }
        public string TargetAttribute { get; set; }

        public ModelCreationViewModel()
        {
            Relations = new string[][]{};
        }

    }
}