using System;

namespace Netlyt.Interfaces.Models
{
    public class Subscription
    {
        public long Id { get; set; }
        public virtual Token AccessToken { get; set; }
        public string Email { get; set; }
        public DateTime Created { get; set; }
        public string ForService { get; set; }
    }
}