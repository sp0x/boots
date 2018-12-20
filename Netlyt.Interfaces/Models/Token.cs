namespace Netlyt.Interfaces.Models
{
    public class Token
    {
        public long Id { get; set; }
        public string Value { get; set; }
        public bool IsUsed { get; set; }
    }
}