using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
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
            var configurationSection = Configuration.GetSection("mail");
            var server = configurationSection["smtp_server"];
            var username = configurationSection["username"];
            var password = configurationSection["password"];
            var fromEmail = configurationSection["from_email"];
            var client = new SmtpClient(server);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(username, password);
            client.Port = int.Parse(configurationSection["smtp_port"]);
            client.EnableSsl = true;

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail);
            mailMessage.To.Add("vaskovasilev94@yahoo.com");
            mailMessage.Body = "body mody";
            mailMessage.Subject = "some subject";
            try
            {
                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}",
                    ex.ToString());
            }
        }
    }
}
