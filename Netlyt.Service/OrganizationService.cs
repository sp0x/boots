using System;
using System.Linq;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Http;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;

namespace Netlyt.Service
{
    public class OrganizationService
    {
        //private IHttpContextAccessor _contextAccessor;
        private IDbContextScopeFactory _contextFactory;
        public OrganizationService(IDbContextScopeFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public Organization Get(string modelOrg)
        {
            using (var ctxSrc = _contextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var org = context.Organizations.FirstOrDefault(x => String.Equals(x.Name, modelOrg, StringComparison.CurrentCultureIgnoreCase));
                return org;
            }
        }

        public static bool IsNetlyt(Organization userOrganization)
        {
            return userOrganization.Id == 1 && userOrganization.Name == "Netlyt";
        }
    }
}