namespace Netlyt.Service.Ml
{
    public class ApiUser
    {
        public string UserId { get; set; }
        public User User { get; set; }
        public long ApiId { get; set; }
        public ApiAuth Api { get; set; }

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