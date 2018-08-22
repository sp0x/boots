namespace Netlyt.Interfaces.Models
{
    public class Permission
    {
        public long Id { get; set; }
        public virtual Organization ShareWith { get; set; }
        public virtual Organization Owner { get; set; }
        public bool CanRead { get; set; }
        public bool CanModify { get; set; }
    }
}