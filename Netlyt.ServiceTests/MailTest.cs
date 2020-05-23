using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Netlyt.Service;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests
{
    [Collection("Entity Parsers")]
    public class MailTest
    {
        public IConfigurationRoot Configuration { get; set; }
        public MailTest(ConfigurationFixture cfg)
        {
            Configuration = cfg.Configuration;
        }

        [Fact]
        public void TestSendEmail()
        {
            var svc = new AuthMessageSender(Configuration);
            try
            {
                svc.SendEmailAsync("vaskovasilev94@yahoo.com", "Subject a", "Long message..");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}",
                    ex.ToString());
            }
        }
    }
}
