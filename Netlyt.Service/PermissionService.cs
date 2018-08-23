using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Models;
using Newtonsoft.Json.Linq;



namespace Netlyt.Service
{
    public class PermissionService
    {
        //private IHttpContextAccessor _contextAccessor;
        private ManagementDbContext _context;
        public PermissionService(ManagementDbContext context)
        {
            _context = context;
        }

        public Permission Get(long id)
        {
            var perm = _context.Permissions.FirstOrDefault(x => x.Id == id);
            return perm;
        }

        public async Task<Permission> Create(Organization Owner, Organization shareWtih, bool canRead, bool canModify){
            var perm = new Permission(){Owner = Owner, ShareWith = shareWtih, CanRead = canRead, CanModify = canModify };
            _context.Permissions.Add(perm);
             try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                throw new Exception("Could not create a new permission!");
            }
            return perm;
        }

        public void Update(Permission perm, bool canRead, bool canModify){
            perm.CanRead = canRead;
            perm.CanModify = canModify;
            _context.SaveChanges();
        }

        public void DeletePermission(Permission perm)
        {
            _context.Permissions.Remove(perm);
            _context.SaveChanges();
        }
    }
}