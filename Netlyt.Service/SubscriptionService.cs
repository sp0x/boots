using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityFramework.DbContextScope.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Exceptions;

namespace Netlyt.Service
{
    public class SubscriptionService : ISubscriptionService
    {
        private IDbContextScopeFactory _dbContextFactory;
        private IEmailSender _emailer;

        public SubscriptionService(IDbContextScopeFactory dbContextFactory, IEmailSender emailer)
        {
            _dbContextFactory = dbContextFactory;
            _emailer = emailer;
        }

        public async Task<Token> SubscribeForAccess(string email, string forService = "Netlyt", bool sendNotification = true)
        {
            var tok = new Token();
            using (var dbFactory = _dbContextFactory.Create())
            {
                var context = dbFactory.DbContexts.Get<ManagementDbContext>();
                if (context.Subscriptions.Any(x => x.Email == email))
                {
                    throw new SubscriptionAlreadyExists(email);
                }
                var sub = new Subscription() {Email = email, Created = DateTime.Now};
                tok.Value = Guid.NewGuid().ToString();
                tok.IsUsed = false;
                sub.AccessToken = tok;
                sub.ForService = forService;
                context.Subscriptions.Add(sub);
                context.SaveChanges();
            }

            if (sendNotification)
            {
                string subject = "Thank you for subscribing.";
                var sb = new StringBuilder();
                sb.AppendLine("Thank you for subscribing.");
                sb.AppendLine("Your access link:");
                sb.AppendLine("https://service.netlyt.com/register?token=" + tok.Value);
                sb.AppendLine("");
                sb.AppendLine("Netlyt");
                await _emailer.SendEmailAsync(email, subject, sb.ToString());
            }
            return tok;
        }
    }
}
