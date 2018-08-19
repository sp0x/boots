using System.Threading.Tasks;

namespace Netlyt.Service.Cloud.Interfaces
{
    public interface ICloudMasterServer
    {
        AuthListener AuthListener { get; }
        NotificationListener NotificationListener { get; }
        bool Running { get; set; }

        Task Run();
    }
}