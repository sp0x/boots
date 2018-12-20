using System.Collections.Generic;

namespace Netlyt.Interfaces.Cloud
{
    public interface ICloudNodeNotification
    {
        string Token { get; }
        Dictionary<string, string> Headers { get; }
    }
}