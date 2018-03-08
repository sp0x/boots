namespace Netlyt.Web.ViewModels
{
    public class ModelCreationViewModel
    {
        public string Name { get; set; }
        public string DataSource { get; set; }
        public string Callback { get; set; }
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

    }
}