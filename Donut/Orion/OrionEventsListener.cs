using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Donut.Orion
{
    public class OrionEventsListener
    {
        public delegate void OrionEventHandler(JObject message);
        private OrionSource _reader;
        public event OrionEventHandler NewMessage;
        public OrionEventsListener()
        {
            _reader = new OrionSource("Ev");
            _reader.OnMessage += ReaderOnMessage;
            _reader.Run();
        }

        private void ReaderOnMessage(object sender, string messageContent)
        {
            try
            {
                JObject message = JObject.Parse(messageContent);
                Task.Run(() =>
                {
                    NewMessage?.Invoke(message);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while handling socket event: " + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destinationIp"></param>
        /// <param name="inputPort"></param>
        /// <param name="outputPort"></param>
        public void Connect(string destinationIp, int port)
        { 
            _reader.Connect(destinationIp, port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destinationIp"></param>
        /// <param name="port"></param>
        public async void ConnectAsync(string destinationIp, int port)
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Connect(destinationIp, port);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not connect to orion context at: {destinationIp}{port}");
                        Thread.Sleep(5000);
                    }
                }
               
            });
        }
    }
}