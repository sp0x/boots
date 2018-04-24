namespace Netlyt.Interfaces
{
    public interface IApiAuth
    {
        long Id { get; set; }
        string AppId { get; set; } 
        string AppSecret { get; set; }
        string Type { get; set; }
    }
}