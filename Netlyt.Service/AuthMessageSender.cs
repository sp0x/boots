using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Donut;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Netlyt.Service;
using Newtonsoft.Json.Linq;

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
            var mailConf = Configuration.GetSection("mail");
            var fromEmail = mailConf["from_email"];
            var mailingType = mailConf["type"]?.ToString();
            if (string.IsNullOrEmpty(mailingType)) mailingType = "smtp";
            if (mailingType == "smtp")
            {
                var server = mailConf["smtp_server"];
                var username = mailConf["username"];
                var password = mailConf["password"];
                var client = new SmtpClient(server);
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(username, password);
                client.Port = int.Parse(mailConf["smtp_port"]);
                client.EnableSsl = true;

                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(fromEmail);
                mailMessage.To.Add(email);
                mailMessage.Body = message;
                mailMessage.Subject = subject;
                client.Send(mailMessage);
            }
            else if(mailingType=="mailjet")
            {
                var mjKey = mailConf["mj.key"].ToString();
                var mjSecret = mailConf["mj.secret"].ToString();
                MailjetClient client = new MailjetClient(mjKey, mjSecret);
                MailjetRequest request = new MailjetRequest
                {
                    Resource = Send.Resource,
                }
                    .Property(Send.FromEmail, fromEmail)
                    .Property(Send.Subject, subject)
                    .Property(Send.TextPart, message)
                    .Property(Send.FromName, "Netlyt");
                var tMail = new JObject();
                tMail["Email"] = email;
                request.Property(Send.Recipients, new JArray { tMail });
                MailjetResponse response = client.PostAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine(string.Format("Total: {0}, Count: {1}\n", response.GetTotal(), response.GetCount()));
                    Console.WriteLine(response.GetData());
                }
                else
                {
                    Console.WriteLine(string.Format("StatusCode: {0}\n", response.StatusCode));
                    Console.WriteLine(string.Format("ErrorInfo: {0}\n", response.GetErrorInfo()));
                    Console.WriteLine(string.Format("ErrorMessage: {0}\n", response.GetErrorMessage()));
                }
            }
            else
            {
                throw new NotImplementedException($"Method of sending emails: {mailingType} not addded!");;
            }
            return Task.FromResult(0);
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}
