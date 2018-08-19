using System;
using System.Threading.Tasks;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud.Auth;

namespace Netlyt.Service.Cloud.Interfaces
{
    public interface IAuthWriter
    {
        Task<AuthenticationResponse> AuthorizeNode(NetlytNode node);
        event EventHandler<AuthenticationResponse> OnAuthenticated;
    }
}
