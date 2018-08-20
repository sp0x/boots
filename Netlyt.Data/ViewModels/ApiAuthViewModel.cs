namespace Netlyt.Data.ViewModels
{
    public class ScriptViewModel
    {
        public ScriptAssetsViewModel Data { get; set; }
    }

    public class ScriptAssetsViewModel
    {
        public string Code { get; set; }
        public string Script { get; set; }
        public bool UseScript { get; set; }
    }

    public class ApiAuthViewModel
    {
        public long Id { get;set; }
        public string AppId { get; set; }
    }
}