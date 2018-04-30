using System;
using System.Collections.Generic;

namespace Netlyt.Interfaces
{
    public class ApiPermissionsSet
    {
        public long Id { get; set; }
        public ICollection<ApiPermission> Required { get; set; }
        public string Type { get; set; }

        public ApiPermissionsSet()
        {
            Required = new HashSet<ApiPermission>();
        }

        public ApiPermissionsSet Add(ApiPermission permission)
        {
            Required.Add(permission);
            return this;
        }
        public ApiPermissionsSet AddAll(ApiPermission[] perms)
        {
            Array.ForEach(perms, x => Required.Add(x));
            return this;
        }
    }
}