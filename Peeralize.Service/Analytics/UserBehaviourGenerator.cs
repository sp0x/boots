using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bogus;
using Bogus.DataSets;
using Peeralize.Service.Models;

namespace Peeralize.Service.Analytics
{ 
    public class UserBehaviourGenerator
    {

        private EndUser _user;
        private string _domain;

        public UserBehaviourGenerator()
        {
            _domain = (new Faker()).Internet.DomainName();
        }

        public UserBehaviourGenerator(EndUser user) : this()
        {
            _user = user;
        } 
         

        public EndUserBehaviour Generate()
        {
            return GenerateBehaviour(1).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<EndUserBehaviour> GenerateBehaviour(int count)
        {
            var userIds = 0;
            var joined = _user.GetProperty<DateTime>("Joined");
            var lastOnline = _user.LastOnline;
            var testUsers = new Faker<EndUserBehaviour>()
                //Optional: Call for objects that have complex initialization
                .CustomInstantiator(f => new EndUserBehaviour()
                {
                    Id = userIds++
                })
                //Basic rules using built-in generators
                .RuleFor(u => u.Type, f => f.PickRandom<BehaviourType>())
                .RuleFor(u => u.Duration, f => f.Random.Number(500)) 
                .RuleFor(u => u.UserId , _user.Id)
                .RuleFor(u => u.Created, (f, u) => f.Date.Between(joined, lastOnline.AddHours(1)))
                .RuleFor(u => u.Referrer, (f) => f.Web.FullUrl())
                .RuleFor(u => u.Url, (f) => f.Web.FullUrl(_domain))
                .FinishWith((f, u) => 
                {
                });
            var generateUsers = testUsers.Generate(count);
            return generateUsers;
        }

        public void SetUser(EndUser user)
        {
            _user = user;
        }

        public void SetDomain(string domain)
        {
            _domain = domain;
        }
    }
}