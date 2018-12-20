using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Web.Models.ManageViewModels;
using Netlyt.Data.ViewModels;
using Donut;
using Donut.Data;
using Donut.Models;
using Netlyt.Service.Cloud;

namespace Netlyt.Web.Controllers
{
    [Produces("application/json")]
    [Authorize()]
    public class PermissionController : Controller
    {
        private UserManager<User> _userManager;
        private IUserManagementService _userManagementService;
        private PermissionService _permissionService;
        private IIntegrationService _integrationService;
        private ModelService _modelService;
        private OrganizationService _orgService;
        private INotificationService _notifications;

        public PermissionController(
            IUserManagementService userManagementService,
            UserManager<User> userManager,
            PermissionService permService,
            ModelService modelService,
            IIntegrationService integrationService,
            OrganizationService orgService,
            INotificationService notificationService)
        {
            _notifications = notificationService;
            _userManagementService = userManagementService;
            _userManager = userManager;
            _permissionService = permService;
            _modelService = modelService;
            _integrationService = integrationService;
            _orgService = orgService;
        }

        [HttpPost("/permission/create_permission")]
        [Authorize]
        public async Task<IActionResult> Create([FromBody]NewPermissionViewModel perm){
            var user = await _userManagementService.GetCurrentUser();
            var obj = new object();
            if(perm.ObjectType == "integration"){
                obj = _integrationService.GetById(perm.ObjectId);
            }else if(perm.ObjectType == "model"){
                obj = _modelService.GetById(perm.ObjectId, user);
            }
            if (obj == null){
                return NotFound(string.Format("Object of type = {0} with id = {1} was not found", perm.ObjectType, perm.ObjectId));
            }
            var org = _orgService.Get(perm.Org);
            if (org == null){
                return NotFound(string.Format("Organization {0} was not found", perm.Org));
            }
            var newPerm = await _permissionService.Create(user, perm.Org, perm.CanRead, perm.CanModify);
            if (perm.ObjectType == "integration")
            {
                _permissionService.AddForIntegration(obj as DataIntegration, newPerm);
                newPerm.DataIntegration = obj as DataIntegration;
                newPerm.DataIntegrationId = newPerm.DataIntegration.Id;
            }
            else if (perm.ObjectType == "model")
            {
                _permissionService.AddForModel(obj as Model, newPerm);
                newPerm.Model = obj as Model;
                newPerm.ModelId = newPerm.Model.Id;
            }
            _notifications.SendPermissionCreated(user,newPerm);
            return Json(new
            {
                id = newPerm.Id,
                canModify = newPerm.CanModify,
                canRead = newPerm.CanRead,
                perm.Org,
                shareWith = new
                {
                    name = perm.Org
                }
            });
        }

        [Authorize]
        [HttpPatch("/permission/{id}/")]
        public async Task<IActionResult> SetPermission(long id, [FromBody]NewPermissionViewModel perm){
            var perm1 = _permissionService.Get(id);
            var user = await _userManagementService.GetCurrentUser();
            if(user.Organization != perm1.Owner){
                return Forbid(string.Format("You are not the owner of this permission and cannot modify it"));
            }
            if (perm1 == null){
                return NotFound(string.Format("Permission with id = {0} was not found",id));
            }
            _permissionService.Update(perm1, perm.CanRead, perm.CanModify);
            return Ok();            
        }

        [Authorize]
        [HttpDelete("/permission/{id}")]
        public async Task<IActionResult> Revoke(long id)
        {
            var user = await _userManagementService.GetCurrentUser();
            try
            {
                var deletedPermission = _permissionService.DeletePermission(user, id);
                _notifications.SendPermissionRemoved(user, deletedPermission);
            }
            catch (NotFound nf)
            {
                return NotFound(nf.Message);
            }
            catch (Forbidden f)
            {
                return Forbid(f.Message);
            }
            return new NoContentResult();
        }
    }
}