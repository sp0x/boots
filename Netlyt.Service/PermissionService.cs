using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Donut.Data;
using Donut.Models;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.EntityFrameworkCore;
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Models;
using Netlyt.Service.Repisitories;
using Newtonsoft.Json.Linq;



namespace Netlyt.Service
{
    public class PermissionService
    {
        private IDbContextScopeFactory _dbContextFactory;
        private IIntegrationRepository _integrations;
        private IPermissionRepository _permissions;
        private IModelRepository _models;

        public PermissionService(
            IDbContextScopeFactory dbContextFactory,
            IIntegrationRepository integrations,
            IModelRepository models,
            IPermissionRepository permissions)
        {
            _permissions = permissions;
            _dbContextFactory = dbContextFactory;
            _integrations = integrations;
            _models = models;
        }

        public Permission Get(long id)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var perm = context.Permissions.FirstOrDefault(x => x.Id == id);
                return perm;
            }
        }

        public async Task<Permission> Create(User ownerOrg, string shareWtihOrg, bool canRead, bool canModify)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var org = context.Users.Where(x => x.Id == ownerOrg.Id).Select(x=>x.Organization).FirstOrDefault();
                return await Create(org, shareWtihOrg, canRead, canModify);
            }
        }

        public async Task<Permission> Create(Organization ownerOrg, string shareWtihOrg, bool canRead, bool canModify){
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var targetOrg = context.Organizations.FirstOrDefault(x => x.Name == shareWtihOrg);
                var perm = new Permission() { Owner = ownerOrg, ShareWith = targetOrg, CanRead = canRead, CanModify = canModify };
                context.Permissions.Add(perm);
                try
                {
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    throw new Exception("Could not create a new permission!");
                }
                return perm;
            }
        }

        public void Update(Permission perm, bool canRead, bool canModify){

            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                perm = context.Permissions.FirstOrDefault(x => x.Id == perm.Id);
                perm.CanRead = canRead;
                perm.CanModify = canModify;
                context.SaveChanges();
            }
        }

        public void DeletePermission(User user, long permId)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var perm = context.Permissions.FirstOrDefault(x => x.Id == permId);
                if (perm == null)
                {
                    throw new NotFound("Permission not found.");
                }
                var userOrg = context.Users.Where(x => x.Id == user.Id).Select(x => x.Organization).FirstOrDefault();
                if (userOrg != perm.Owner)
                {
                    throw new Forbidden(string.Format("You are not the owner of this permission and cannot modify it"));
                }
                context.Permissions.Remove(perm);
                context.SaveChanges();
            }
        }

        public void AddForIntegration(DataIntegration dataIntegration, Permission newPerm)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                dataIntegration = _integrations.GetById(dataIntegration.Id).FirstOrDefault();
                newPerm = _permissions.GetById(newPerm.Id);
                dataIntegration?.Permissions.Add(newPerm);
                ctxSrc.SaveChanges();
            }
        }
        public void AddForModel(Model model, Permission newPerm)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                model = _models.GetById(model.Id).FirstOrDefault();
                model?.Permissions.Add(newPerm);
                ctxSrc.SaveChanges();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool CheckAccess(Model item, User user)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var hasPermission = context.Models
                    .Any(m => m.Id == item.Id && m.Permissions.Any(x => x.ShareWith.Id == user.Organization.Id));
                if (!hasPermission)
                {
                    throw new Forbidden("You are not authorized to view this model");
                }
                return true;
            }
        }

        public IEnumerable<Permission> GetForModel(Model model)
        {

            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                return context.Models.FirstOrDefault(x => x.Id == model.Id)?.Permissions.ToList();
            }
        }
    }
}