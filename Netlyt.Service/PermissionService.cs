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
using Netlyt.Service.Cloud.Auth;
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
        private IUserService _userService;

        public PermissionService(
            IDbContextScopeFactory dbContextFactory,
            IIntegrationRepository integrations,
            IModelRepository models,
            IPermissionRepository permissions,
            IUserService userService)
        {
            _userService = userService;
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

        public Permission DeletePermission(User user, long permId)
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
                return perm;
            }
        }

        public void AddForIntegration(DataIntegration dataIntegration, Permission newPerm)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                dataIntegration = _integrations.GetById(dataIntegration.Id).FirstOrDefault();
                newPerm = _permissions.GetById(newPerm.Id);
                newPerm.DataIntegration = dataIntegration;
                dataIntegration?.Permissions.Add(newPerm);
                ctxSrc.SaveChanges();
            }
        }
        public void AddForModel(Model model, Permission newPerm)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                model = _models.GetById(model.Id).FirstOrDefault();
                newPerm.Model = model;
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

        public void OnRemotePermissionUpdated(JsonNotification notification, JToken eBody)
        {
            var type = notification.GetHeader("type");
            switch (type)
            {
                case "create":
                    CreateRemotePermission(notification, eBody);
                    break;
                case "remove":
                    RemoveRemotePermission(notification, eBody);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void RemoveRemotePermission(JsonNotification notification, JToken eBody)
        {
            var remoteId = eBody["id"].Value<long?>();
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                User user = _userService.GetByCloudNodeToken(notification.Token);
                if (user == null || user.Organization == null)
                {
                    throw new NotFound("User or organization not found.");
                }
                long ownerId = user.Organization.Id;
                var localPermission = context.Permissions.FirstOrDefault(x => x.RemoteId == remoteId.Value && x.Owner.Id == ownerId);
                if (localPermission != null)
                {
                    context.Permissions.Remove(localPermission);
                    context.SaveChanges();
                }
            }
        }

        private void CreateRemotePermission(JsonNotification notification, JToken eBody)
        {
            var ownerId = long.Parse(eBody["OwnerId"].ToString());
            var shareWithId = long.Parse(eBody["ShareWith"]["Id"].ToString());
            var remoteIntegrationId = eBody["DataIntegrationId"].Value<long?>();
            var remoteModelId = eBody["ModelId"].Value<long?>();
            long? localIntegrationId = null;
            long? localModelId = null;
            var permission = new Permission();

            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                if (remoteIntegrationId != null)
                {
                    localIntegrationId =
                        context.Integrations.FirstOrDefault(x => x.RemoteId == remoteIntegrationId.Value)?.Id;
                }
                else if (remoteModelId != null)
                {
                    localModelId =
                        context.Models.FirstOrDefault(x => x.RemoteId == remoteModelId.Value)?.Id;
                }
                permission.IsRemote = true;
                permission.RemoteId = long.Parse(eBody["id"].ToString());
                permission.Owner = context.Organizations.FirstOrDefault(x => x.Id == ownerId);
                permission.CanModify = eBody["CanModify"].Value<Boolean>();
                permission.CanRead = eBody["CanRead"].Value<Boolean>();
                permission.DataIntegrationId = localIntegrationId;
                permission.ModelId = localModelId;
                permission.ShareWith = context.Organizations.FirstOrDefault(x => x.Id == shareWithId);
                context.Permissions.Add(permission);
                context.SaveChanges();
            }
        }
    }
}