using System;
using System.Diagnostics;
using NetMQ;
using NetMQ.Sockets;

namespace Netlyt.Service.Orion
{
    /// <summary>
    /// A zeromq data sink (push stream)
    /// </summary>
    public class OrionSource
    {
        /// <summary>
        /// The pull socket
        /// </summary>
        public PullSocket Socket { get; private set; }
        private NetMQPoller _poller;

        public event EventHandler<string> OnMessage;
        /// <summary>
        /// 
        /// </summary>
        public OrionSource()
        {
            Socket = new PullSocket();
            _poller = new NetMQPoller();
            _poller.Add(Socket);
            Socket.ReceiveReady += OnDataAvailable;
            _poller.RunAsync();
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
            //Debug.WriteLine("Received frame: " + frame);
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
