using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;

namespace Peeralize.Service.Network
{
    /// <summary>
    /// A zeromq data sink (push stream)
    /// </summary>
    public class DataSink
    {
        /// <summary>
        /// The push socket
        /// </summary>
        public PushSocket Socket { get; private set; }

        public DataSink()
        {
            Socket = new PushSocket();
        }

        public void Connect(string destination)
        {
            Socket.Connect(destination);
        }

        /// <summary>
        /// Connects to the destination
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="port"></param>
        public void Connect(string destination, int port)
        {
            Connect($"tcp://{destination}:{port}");
        }

        public void Send(byte[] data)
        { 
            Socket.SendFrame(data); 
        }

        public void Send(JToken token)
        {
            Send(token.ToString());
        }
        /// <summary>
        /// Sends a frame sting
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            Socket.SendFrame(data); 
        }
    }
}
