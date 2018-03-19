using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Orion
{
    public class OrionEventsListener
    {
        public delegate void OrionEventHandler(JObject message);
        private OrionSource _reader;
        public event OrionEventHandler NewMessage;
        public OrionEventsListener()
        {
            _reader = new OrionSource();
            _reader.OnMessage += ReaderOnMessage;
        }

        private void ReaderOnMessage(object sender, string messageContent)
        {
            JObject message = JObject.Parse(messageContent);
            NewMessage?.Invoke(message);
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
        /// <param name="inputPort"></param>
        /// <param name="outputPort"></param>
        public async void ConnectAsync(string destinationIp, int port)
        {
            await Task.Run(() =>
            {
                Connect(destinationIp, port);
            });
        }
    }
}