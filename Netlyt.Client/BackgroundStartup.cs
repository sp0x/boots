using System;
using Netlyt.Service;
using Netlyt.Service.Cloud;
using Netlyt.Service.Cloud.Slave;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
namespace Netlyt.Client
{
    public partial class Startup
    {
        public DonutOrionHandler OrionHandler { get; set; }
        public ISlaveConnector SlaveConnector { get; private set; }

        public void ConfigureBackgroundServices(IServiceProvider mainServices)
        {
            var dbConfig = Configuration.GetDbOptionsBuilder();
            NetlytService.SetupBackgroundServices(dbConfig, Configuration, OrionContext);
        }

    }
}
