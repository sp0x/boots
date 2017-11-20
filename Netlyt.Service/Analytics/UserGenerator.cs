using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bogus;
using Bogus.DataSets;
using Netlyt.Service.Models;

namespace Netlyt.Service.Analytics
{
    public enum Gender
    {
        Male, Female
    }
    public class UserGenerator
    {
        private List<string> _firstNames;
        private List<string> _lastNames; 
        private Bogus.DataSets.Imdb _wordset; 

        public UserGenerator()
        {  
            _wordset = new Bogus.DataSets.Imdb(); 
        } 

        private String[] GenerateLikes()
        {
            var rand = new Random();
            var count = rand.Next(2, 50);
            var posts = _wordset.Sentences(count);
            return posts;
        }
        private String[] GeneratePosts()
        {
            var rand = new Random();
            var count = rand.Next(2, 20);
            var posts = _wordset.Sentences(count);
            return posts;
        }
         
        public EndUser Generate()
        {
            return GenerateUsers(1).FirstOrDefault();
        }

        /// <summary>
        /// Generates a collection of random users
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<EndUser> GenerateUsers(int count)
        {
            var userIds = 0;
            var testUsers = new Faker<EndUser>()
                //Optional: Call for objects that have complex initialization
                .CustomInstantiator(f => new EndUser()
                {
                    Id = userIds++
                })
                //Basic rules using built-in generators
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName()) 
                .RuleFor(u => u.UserName, (f, u) => f.Internet.UserName(u.FirstName, u.LastName))
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName)) 
                .RuleFor(u => u.Birthday, f=> f.Date.Past(80))
                //Use an enum outside scope.
                .RuleFor(u => u.Gender, f => f.PickRandom<Gender>())
                .RuleFor(u => u.FullAddress, f => f.Address.FullAddress())
                .RuleFor(u => u.Email, (f, u ) => f.Internet.Email(u.FirstName, u.LastName))
                .RuleFor(u => u.Occupation, f=> f.Occupation.Get())
                .Rules((f, u) => u.SetProperty("CurrentCompany", f.Company.CompanyName()))
                .Rules((f, u) => u.SetProperty("Posts", GeneratePosts()))
                .Rules((f, u) => u.SetProperty("Likes", f.Company.CompanyNames()))
                .Rules((f, u) => u.SetProperty("PastJobsCount", f.Random.Int(1, 100)))
                .Rules((f, u) => u.SetProperty("Joined" , f.Date.Future(16, u.Birthday)))
                .Rules((f,u) => u.LastOnline = f.Date.Future(3,u.GetProperty<DateTime>("Joined")))
                .Rules((f, u) => u.SetProperty("PaymentCount", f.Random.Number(15)))
                .Rules((f, u) => u.SetProperty("Subscribed", f.Date.Future(1, u.Properties["Joined"] as DateTime?)))
                .FinishWith((f, u) => 
                {
                });
            var generateUsers = testUsers.Generate(count);
            return generateUsers;
        }

    }
}