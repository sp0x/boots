using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;

namespace Netlyt.Service
{
    public class OrganizationService
    {
        //private IHttpContextAccessor _contextAccessor;
        private ManagementDbContext _context;
        public OrganizationService(ManagementDbContext context)
        {
            _context = context;
        }

        public Organization Get(string modelOrg)
        {
            var org = _context.Organizations.FirstOrDefault(x => String.Equals(x.Name, modelOrg, StringComparison.CurrentCultureIgnoreCase));
            return org;
        }
    }
}