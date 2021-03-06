﻿using System;
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
                var link = "https://service.netlyt.io/register?token=" + tok.Value;
                sb.AppendLine(@"
This is Tony, co-founder at Netlyt. I'm glad that you decided to give OneClick a try.
You can use OneClick to easily create ML models and bring ML to your organisation in a matter of days.
Here is your link: " + link + @"
Just as an example, with OneClick you can:
- Churn
- Conversion

If you have any questions, don't hesitate to write me.

Tony
");
                await _emailer.SendEmailAsync(email, subject, sb.ToString());
            }
            return tok;
        }
    }
}
