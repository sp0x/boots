namespace Netlyt.Web.ViewModels
{
    public class NewModelIntegrationViewmodel
    {
        public string Target { get; set; }
        public string IdColumn { get; set; }
        public string Name { get; set; }
        public string UserEmail { get; set; }

    }

    public class NewIntegrationViewModel
    {
        public string Name { get; set; }
        public string DataFormatType { get; set; }
        public string OriginType { get; set; }
    }
}