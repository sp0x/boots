namespace Netlyt.Interfaces.Models
{
    public class UserPermission
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public virtual User User { get; set; }
    }
}