using Netlyt.Interfaces.Models;

namespace Netlyt.Interfaces
{
    public class ApiUser
    {
        public string UserId { get; set; }
        public virtual User User { get; set; }
        public long ApiId { get; set; }
        public virtual ApiAuth Api { get; set; }

        public ApiUser()
        {

        }
        public ApiUser(User user, ApiAuth api)
        {
            this.User = user;
            this.Api = api;
        }
    }
}