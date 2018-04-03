using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Netlyt.Service;

namespace Netlyt.Service
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private IConfiguration Configuration { get; set; }

        public AuthMessageSender(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Plug in your email service here to send an email.
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
            mailMessage.To.Add(email);
            mailMessage.Body = message;
            mailMessage.Subject = subject;
            client.Send(mailMessage);
            return Task.FromResult(0);
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}
