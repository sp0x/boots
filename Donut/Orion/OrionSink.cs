using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json.Linq;

namespace Donut.Orion
{
    /// <summary>
    /// A zeromq data sink (push stream)
    /// </summary>
    public class OrionSink
    {
        private string _destination;
        private int _port;

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
            _destination = destination;
            _port = port;
            Connect($"tcp://{_destination}:{_port}");
        }

        public override string ToString()
        {
            return $"tcp://{_destination}:{_port}";
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

        public static string GetExperimentsPath(IConfiguration configuration, out bool isAbs, ref string assetPath)
        {
//            if (assetPath.StartsWith("/experiments/"))
//            {
//                assetPath = assetPath.Substring(13);
//            }
            isAbs = false;
            var cfg = configuration["experiments_path"];
            if (cfg!=null && cfg.Contains("experiments_path_abs"))
            {
                isAbs = configuration["experiments_path_abs"].ToString().ToLower() == "true";
            }
            var envExperiments = Environment.GetEnvironmentVariable("EXP_DIR");
            if (!string.IsNullOrEmpty(envExperiments))
            {
                cfg = envExperiments;
            }
            var envExpDirAbs = Environment.GetEnvironmentVariable("EXP_DIR_ABS");
            if (!string.IsNullOrEmpty(envExpDirAbs))
            {
                cfg = envExpDirAbs;
                isAbs = true;
            }
            var osNameAndVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            var isWindows = osNameAndVersion.Contains("Windows");
            if (isWindows)
            {
                assetPath = assetPath.Replace("/", "\\");
                cfg = cfg.Replace("/", "\\");
            }
            assetPath = assetPath.Trim('\\');

            var cwd = Environment.CurrentDirectory;
            var fullExpPath = !isAbs ? System.IO.Path.Combine(cwd, cfg) : cfg;
            return fullExpPath;
        }
    }
}
