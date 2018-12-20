using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Netlyt.Interfaces
{
    public class MqConfig
    {
        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }

        public static MqConfig GetConfig(IConfiguration configuration)
        {
            var output = new MqConfig();
            var mhost = Environment.GetEnvironmentVariable("MQ_HOST");
            if (string.IsNullOrEmpty(mhost)) mhost = "localhost";
            var mport = Environment.GetEnvironmentVariable("MQ_PORT");
            if (string.IsNullOrEmpty(mport)) mport = "5672";
            var muser = Environment.GetEnvironmentVariable("MQ_USER");
            var mpass = Environment.GetEnvironmentVariable("MQ_PASS");
            output.Host = mhost;
            output.Port = int.Parse(mport);
            output.User = muser;
            output.Password = mpass;
            return output;
        }

        public override string ToString()
        {
            return $"{Host}:{Port}";
        }
    }
}
