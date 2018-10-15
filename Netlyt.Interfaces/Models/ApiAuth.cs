using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace Netlyt.Interfaces.Models
{
    /// <summary>
    /// Represents a universal API OAuth object, holding authentication tokens.
    /// Also contains type information and endpoint, depending on the API that it's used with.
    /// </summary>
    public class ApiAuth : IApiAuth
    {
        //[Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        public virtual ICollection<ApiPermissionsSet> Permissions { get; set; }


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
            var elements = (new string[]
            {
                Endpoint,
                AppId,
                AppSecret,
//                ConsumerKey,
//                ConsumerSecret,
                Type
            });
            int hash = 0;
            foreach (var elem in elements)
            {
                if (elem == null) continue;
                hash = hash ^ elem.GetHashCode();
            }
            return hash;
        }

        public static ApiAuth GetDefault()
        {
            return new ApiAuth()
            {
                AppId = "2078f6d6dc54480cb5e29b7aedd3c95b",
                AppSecret = "lP56JSEYCN2dcOjfmUCt3/PK+uBY9aLV0buTjzdIjNE="
            };
        }
    }
}
