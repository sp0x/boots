using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;

using grpc = global::Grpc.Core;

namespace Peeralize.Service.Network
{
    public class GrpcFactory
    {
        /// <summary>
        /// Creates a new instance of the type, using a given endpoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static T Create<T>(Uri endpoint)
            where T : grpc::ClientBase<T>, new()
        {
            var chan = new Channel(endpoint.ToString(), ChannelCredentials.Insecure);
            var outp = typeof(T).GetConstructor(new Type[] {typeof(Channel)}).Invoke(null, new object[] {chan}) as T;
            return outp;
        }

        /// <summary>
        /// Creates a new instance of the type, using a given endpoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static T Create<T>(string address, int port)
            where T : grpc::ClientBase<T>, new()
        {
            var builder = new UriBuilder();
            builder.Port = port;
            builder.Host = address;
            return Create<T>(builder.Uri);
        }
    }
}
