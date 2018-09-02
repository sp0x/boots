using System.Collections.Generic;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Cloud;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud.Auth;

namespace Netlyt.Service.Cloud
{
    public interface ICloudNodeService
    {
        NetlytNode ResolveLocal();
        bool ShouldSync(string dataType, ICloudNodeNotification jsonNotification);
        bool UserHasOnPremInstance(User src);
    }
}