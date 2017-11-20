using System;
using System.Diagnostics;
using NetMQ;
using NetMQ.Sockets;

namespace Netlyt.Service.Network
{
    /// <summary>
    /// A zeromq data sink (push stream)
    /// </summary>
    public class DataProducer
    {
        /// <summary>
        /// The pull socket
        /// </summary>
        public PullSocket Socket { get; private set; }

        public event EventHandler<string> OnMessage;
        /// <summary>
        /// 
        /// </summary>
        public DataProducer()
        {
            Socket = new PullSocket();
            Socket.ReceiveReady += OnDataAvailable;
        }

        /// <summary>
        /// Data available handling, invokes OnMessage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDataAvailable(object sender, NetMQSocketEventArgs e)
        {
            string frame = e.Socket.ReceiveFrameString();
            //var inpuMessage = e.Socket.ReceiveMultipartMessage(); 
            Debug.WriteLine("Received frame: " + frame);
            OnMessage?.Invoke(this, frame); 
        }

        /// <summary>
        /// Blocks for a frame
        /// </summary>
        /// <returns></returns>
        public string Receive()
        {
            var frame = Socket.ReceiveFrameString();
            return frame;
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
        
        
    }
}
