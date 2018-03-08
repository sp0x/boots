using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;

namespace Netlyt.Service.Orion
{
    /// <summary>
    /// A zeromq data sink (push stream)
    /// </summary>
    public class OrionSink
    {
        /// <summary>
        /// The push socket
        /// </summary>
        public PushSocket Socket { get; private set; }

        public OrionSink()
        {
            Socket = new PushSocket(); 
        }

        /// <summary>   Connects. </summary>
        ///
        /// <remarks>   Vasko, 13-Dec-17. </remarks>
        ///
        /// <param name="destination">  . </param>

        public void Connect(string destination)
        {
            Socket.Connect(destination);
        }

        /// <summary>
        /// Connects to the destination using tcp://{destination}:{port}
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="port"></param>
        public void Connect(string destination, int port)
        {
            Connect($"tcp://{destination}:{port}");
        }

        /// <summary>   Send a raw byte array. </summary>
        ///
        /// <remarks>   Vasko, 13-Dec-17. </remarks>
        ///
        /// <param name="data"> . </param>

        public void Send(byte[] data)
        { 
            Socket.SendFrame(data); 
        }

        /// <summary>   Send this JToken as a string. </summary>
        ///
        /// <remarks>   Vasko, 13-Dec-17. </remarks>
        ///
        /// <param name="token">    The token. </param>

        public void Send(JToken token)
        {
            Send(token.ToString()); 
        }
        /// <summary>
        /// Sends the raw data string.
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            if (!Socket.TrySendFrame(data))
            {
                var x = 1;
                x++;
            }
        }
    }
}
