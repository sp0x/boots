using System;
using System.Collections.Generic;
using nvoid.Documents;

namespace Netlyt.Service.Models
{
    public class ExtendableObject
    {
        public Dictionary<string, object> Properties { get; set; }

        public ExtendableObject()
        {
            this.Properties = new Dictionary<string, object>();
        }

        public void SetProperty(string key, object val)
        {
            if (Properties.ContainsKey(key)) Properties[key] = val;
            else
            {
                Properties.Add(key, val);
            }
        }
        /// <summary>
        /// Tries to get a property, otherwise returns null
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetProperty(string key)
        {
            if (Properties.ContainsKey(key))
            {
                return Properties[key];
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Tries to get a property, otherwise returns null
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetProperty<T>(string key)
        {
            if (Properties.ContainsKey(key))
            {
                return (T)Properties[key];
            }
            else
            {
                return default(T);
            }
        }

        public object this[string key]
        {
            get
            {
                if (Properties.ContainsKey(key)) return Properties[key];
                else return null;
            }
            set
            {
                if (Properties.ContainsKey(key))
                {
                    Properties.Add(key, value);
                }
                else
                {
                    Properties[key] = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual EntityDocument GetXlsConverter()
        {
            var converter = new XlsConverter<ExtendableObject>();
            
            foreach (var key in Properties.Keys)
            {
                Func<ExtendableObject, object> getter = (x) => x[key];
                converter.Members.Add(key, getter);
            }
            converter.Ignore(x => x.Properties).Ignore("Item");
            return converter;
        }
    }
}