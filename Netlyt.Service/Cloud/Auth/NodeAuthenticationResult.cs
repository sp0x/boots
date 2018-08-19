using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Cloud.Auth
{
    public class NodeAuthenticationResult
    {
        public bool Authenticated { get; private set; }
        public string Token { get; private set; }
        public User User { get; set; }

        public NodeAuthenticationResult()
        {
            Authenticated = false;
        }

        public NodeAuthenticationResult(string token, User user)
        {
            this.Token = token;
            this.User = user;
            this.Authenticated = true;
        }
    }
}