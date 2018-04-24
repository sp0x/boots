using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using nvoid.db.DB;
using nvoid.db.Extensions;
using nvoid.Integration;
using Netlyt.Interfaces;
using Netlyt.Service.Ml;

namespace Netlyt.Service
{
    /// <summary>
    /// Represents a universal API OAuth object, holding authentication tokens.
    /// Also contains type information and endpoint, depending on the API that it's used with.
    /// </summary>
    public class ApiAuth : Entity, IApiAuth
    {
        public long Id { get; set; }
        public string Endpoint { get; set; }
        /// <summary>
        /// API key or Application Key
        /// </summary>
        /// <returns></returns>
        [Required]
        public string AppId { get; set; }
        /// <summary>
        /// The application key secret
        /// </summary>
        public string AppSecret { get; set; }
//        public string ConsumerKey { get; set; }
//        public string ConsumerSecret { get; set; }
        public virtual ICollection<ApiUser> Users { get; set; }
        public string Type { get; set; } 
        /// <summary>
        /// 
        /// </summary>
        public ICollection<ApiPermissionsSet> Permissions { get; set; }


        public ApiAuth()
        {
            Permissions = new HashSet<ApiPermissionsSet>();
            Users = new HashSet<ApiUser>();
        }

        /// <summary>
        /// Compares the API keys if they're equal, by 100% match.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is ApiAuth)
            {
                return obj.GetHashCode() == this.GetHashCode();
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public static ApiAuth Generate()
        {
            using (var cryptoProvider = new RNGCryptoServiceProvider())
            {
                byte[] btSecret = new byte[32];
                cryptoProvider.GetBytes(btSecret);
                var apiSecretKey = Convert.ToBase64String(btSecret);
                var apiId = Guid.NewGuid().ToString("N");
                var auth = new ApiAuth()
                {
                    AppId = apiId,
                    AppSecret = apiSecretKey
                };
                return auth;
            }
        }

        public override int GetHashCode()
        {
            return (new string[] {
                Endpoint,
                AppId,
                AppSecret,
//                ConsumerKey,
//                ConsumerSecret,
                Type
            }).GetUnorderedHashcode();
        }
    }
}
