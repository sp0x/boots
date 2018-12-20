using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Netlyt.Service.Cloud.Slave;
using Netlyt.Service.Data;

namespace Netlyt.Web
{
    public class Program
    {
        public static ISlaveConnector SlaveConnector { get; private set; }
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseSetting("detailedErrors", "true")
                .UseUrls("http://0.0.0.0:81/")
                .CaptureStartupErrors(true)
                .UseStartup<Startup>()
                .Build();
            return host;
        }
            
    }
}
