using System;
using System.ComponentModel.DataAnnotations.Schema;
using Donut.Interfaces.Models;

namespace Netlyt.Interfaces.Models
{
    public class ActionLog
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }
        public string Value { get; set; }
        public string InstanceToken { get; set; }
        public ActionLogType Type { get; set; }
        public long ObjectId { get; set; }
    }

    public enum ActionLogType
    {
        IntegrationCreated, IntegrationViewed, ModelCreated, ModelEdited, ModelTrained, ModelStageUpdate, UserLoggedIn, UserRegistered,
        QuotaSynced,
        PermissionsSet,
        PermissionsCreate,
        PermissionRemoved
    }
}