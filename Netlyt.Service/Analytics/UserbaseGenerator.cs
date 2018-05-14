using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bogus;
using Netlyt.Interfaces;
using Netlyt.Service.Models;

namespace Netlyt.Service.Analytics
{
    /// <summary>
    /// 
    /// </summary>
    public class UserbaseGenerator
    {
        //private FBManager _network;
        public UserbaseGenerator()
        {
            //_network = new FBManager();
        }

        /// <summary>
        /// Authorizes the provider network with a given api key
        /// </summary>
        /// <param name="auth"></param>
        /// <returns>True if everything went ok.</returns>
        public bool SetApiKey(ApiAuth auth)
        {
            //return _network.AuthorizeWith(auth);
            return false;
        }

        public IEnumerable<EndUser> GetUsers(int limit)
        {
            //SocialAccount myUser = _network.GetMyUser();
            //var friends = _network.GetMyFriends();
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destinationDirectory"></param>
        public void Simulate(string destinationDirectory, int userCount = 10)
        {
            //PrepareExtraction
            var userGen = new UserGenerator();
            var behaviourGen = new UserBehaviourGenerator();
            var domain = (new Faker()).Internet.DomainName();
            behaviourGen.SetDomain(domain);
            var randBehaviour = new Random();
            //Execute 
            foreach (EndUser user in userGen.GenerateUsers(userCount))
            {
                behaviourGen.SetUser(user); 
                var count = randBehaviour.Next(2, 100);
                var behaviour = behaviourGen.GenerateBehaviour(count);
                CreateUser(destinationDirectory, user, behaviour);
            }
        }

        public string CreateUser(string baseDir, EndUser user, IEnumerable<EndUserBehaviour> behaviour)
        {
            var destinationDir = Path.Combine(baseDir);
            var destinationFile = Path.Combine(destinationDir, "profile.xlsx");
            //XlsConverter<EndUser> converter = new XlsConverter<EndUser>(user.GetXlsConverter());
           // converter.Append = true;
            //converter.Convert(user, destinationFile, "User");
            if (behaviour != null)
            {
                var behaviourFile = Path.Combine(destinationDir, "behaviour.xlsx");
                //var behaviourConverter = new XlsConverter<EndUserBehaviour>(behaviour.FirstOrDefault().GetXlsConverter());
                //behaviourConverter.Append = true;
                //behaviourConverter.Convert(behaviour, behaviourFile, "Behaviour");
            }

            return destinationDir;
        }

        /// <summary>
        /// Creates a user directory filled with the given content.
        /// </summary>
        /// <param name="baseDir"></param>
        /// <param name="user"></param>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        private string CreateUserDirectory(string baseDir, EndUser user, IEnumerable<EndUserBehaviour> behaviour)
        {
            var destinationDir = Path.Combine(baseDir, user.Id.ToString());
            var destinationFile = Path.Combine(destinationDir, "profile.xlsx");
            try
            {
                Directory.CreateDirectory(destinationDir);
            }
            catch (Exception)
            {
                return null;
            } 
           // XlsConverter<EndUser> converter = new XlsConverter<EndUser> (user.GetXlsConverter());
            //converter.Convert(user, destinationFile, "User");
            if (behaviour != null)
            {
                var behaviourFile = Path.Combine(destinationDir, "behaviour.xlsx");
                //var behaviourConverter = new XlsConverter<EndUserBehaviour>(behaviour.FirstOrDefault().GetXlsConverter());
                //behaviourConverter.Convert(behaviour, behaviourFile, "Behaviour");
            }

            return destinationDir;
        }

    }
}