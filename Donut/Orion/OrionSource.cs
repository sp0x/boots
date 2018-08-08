using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace Donut.Orion
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
        private NetMQTimer _pinger;
        public string Tag { get; set; }

        public event EventHandler<string> OnMessage;
        /// <summary>
        /// 
        /// </summary>
        public OrionSource(string tag)
        {
            Socket = new PullSocket();
            this.Tag = tag;
            _pinger = new NetMQTimer(TimeSpan.FromSeconds(2));
            _poller = new NetMQPoller { Socket, _pinger };
            Socket.ReceiveReady += OnDataAvailable;
            _pinger.Elapsed += (s, a) =>
            {
                ///Console.WriteLine($"{Tag} Pinger - " + DateTime.Now.ToString());
            };
        }

        public Task Run()
        {
            return Task.Run(() =>
            {
                _poller.Run();
                _poller = _poller;
            });
        }

        /// <summary>
        /// Data available handling, invokes OnMessage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDataAvailable(object sender, NetMQSocketEventArgs e)
        {
            string frame = e.Socket.ReceiveFrameString();
            //e.Socket.TrySendFrame("ack");
            //var inpuMessage = e.Socket.ReceiveMultipartMessage(); 
            Console.WriteLine("Received frame: " + frame);
            OnMessage?.Invoke(this, frame);
        }

        /// <summary>
        /// Blocks for a frame
        /// </summary>
        /// <returns></returns>
        public string Receive()
        {
            var frame = Socket.ReceiveFrameString();
            Socket.TrySendFrame("ack");
            return frame;
        }

        public void Connect(string destination)
        {
            Console.WriteLine($"Connecting to Orion Node: {destination}");
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
