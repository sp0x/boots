using Netlyt.Service.Cloud.Auth;

namespace Netlyt.Service.Cloud.Interfaces
{
    public interface ICloudAuthenticationService
    {
        NodeAuthenticationResult Authenticate(AuthenticationRequest authRequest);
    }
}