namespace Netlyt.Data.ViewModels
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

    public class NewPermissionViewModel{
        public string Org { get; set; }
        public bool CanRead { get; set; }
        public bool CanModify { get; set; }
        public string ObjectType { get; set; }
        public long ObjectId { get; set; }
    }
}