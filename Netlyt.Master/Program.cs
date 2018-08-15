using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Netlyt.Interfaces;
using Netlyt.Service.Cloud;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Netlyt.Master
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            Configure();
            var server = new MasterServer(Configuration);
            var runTask = server.Run();
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static void Configure()
        {
            var cfgBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = cfgBuilder.Build();
        }

        
    }
}
