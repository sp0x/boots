using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Donut.Data;
using Donut.Models;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        private IModelRepository _models;

        public PermissionService(
            IDbContextScopeFactory dbContextFactory,
            IIntegrationRepository integrations,
            IModelRepository models)
        {
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

        public async Task<Permission> Create(Organization Owner, Organization shareWtih, bool canRead, bool canModify){
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var perm = new Permission() { Owner = Owner, ShareWith = shareWtih, CanRead = canRead, CanModify = canModify };
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

        public void DeletePermission(Permission perm)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                perm = context.Permissions.FirstOrDefault(x => x.Id == perm.Id);
                if (perm != null)
                {
                    context.Permissions.Remove(perm);
                    context.SaveChanges();
                }
            }
        }

        public void AddForIntegration(DataIntegration dataIntegration, Permission newPerm)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                dataIntegration = _integrations.GetById(dataIntegration.Id).FirstOrDefault();
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
    }
}