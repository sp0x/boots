using System;
using System.Collections.Generic;
using Netlyt.Service.Analytics;

namespace Netlyt.Service.Models
{
    /// <summary>
    /// An end user, which is the second level user.
    /// </summary>
    public class EndUser : ExtendableObject
    {
        public int Id { get; set; }
        /// <summary>
        /// Reserved for generic properties that a end user might have
        /// </summary>

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Alias { get; set; }
        public DateTime Birthday { get; set; }
        public DateTime LastOnline { get; set; }
        /// <summary>
        /// The entity documents that are related to this user
        /// </summary>
        public List<string> RelatedDocuments { get; set; }

        public string FullAddress { get; set; }
        public string Occupation { get; set; }
        public Gender Gender { get; set; }
        public string UserName { get; set; }

        public EndUser() : base()
        {
        }
        

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <returns></returns>
//        public override EntityDocument GetXlsConverter()
//        {
//            var converter = new XlsConverter<EndUser>(base.GetXlsConverter());
//            converter.MapAllProperties().Ignore(x => x.Alias).Ignore(x => x.RelatedDocuments);
//            return converter;
//        }
//        /// <summary>
//        /// 
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <returns></returns>
//        public virtual EntityDocument GetXlsConverter()
//        {
//            var converter = new XlsConverter<ExtendableObject>();
//
//            foreach (var key in Properties.Keys)
//            {
//                Func<ExtendableObject, object> getter = (x) => x[key];
//                converter.Members.Add(key, getter);
//            }
//            converter.Ignore(x => x.Properties).Ignore("Item");
//            return converter;
//        }
    }
}