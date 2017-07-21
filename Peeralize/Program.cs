using System.IO; 
using Microsoft.AspNetCore.Hosting;

namespace Peeralize
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseSetting("detailedErrors", "true")
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseUrls("http://0.0.0.0/")
                .CaptureStartupErrors(true)
                .Build();

            host.Run();
        }
    }
}
