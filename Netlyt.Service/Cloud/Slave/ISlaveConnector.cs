using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Cloud.Slave
{
    public interface ISlaveConnector : IHostedService
    { 
        ApiRateLimit Quota { get; }
        bool Running { get; set; }
        NodeAuthClient AuthenticationClient { get; }
        NotificationClient NotificationClient { get; }
        Task Run();
        void Send(string message);
    }
}