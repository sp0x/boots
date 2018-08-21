using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Cloud
{
    public interface ICloudNodeService
    {
        NetlytNode ResolveLocal();
    }
}