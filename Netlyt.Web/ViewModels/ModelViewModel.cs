namespace Netlyt.Web.ViewModels
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

    public class ModelUpdateViewModel
    {
        public long Id { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string CallbackUrl { get; set; }
    }
    public class ModelViewModel
    {
        public long Id { get; set; }
        public string ModelName { get; set; }
        public string ClassifierType { get; set; }
        public string CurrentModel { get; set; }
        public string Callback { get; set; }
        public string TrainingParams { get; set; }
        public string HyperParams { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string Endpoint { get; set; }
        public bool IsBuilding { get; set; }
        public string Status { get; set; }
        public ModelTrainingPerformanceViewModel Performance { get; set; }

    }
}