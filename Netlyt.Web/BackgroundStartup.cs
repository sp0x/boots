using System;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;

namespace Netlyt.Web
{
    public partial class Startup
    {
        public DonutOrionHandler OrionHandler { get; set; }
        public void ConfigureBackgroundServices(IServiceProvider mainServices)
        {
            var dbConfig = Configuration.GetDbOptionsBuilder();
            NetlytService.SetupBackgroundServices(dbConfig, Configuration, OrionContext);
        }

    }
}
