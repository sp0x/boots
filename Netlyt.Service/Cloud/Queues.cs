using System;
using System.Collections.Generic;
using System.Text;

namespace Netlyt.Service.Cloud
{
    public static class Queues
    {
        public const string Notification = "Notification";
        public const string Request = "Request";
        public const string MessageNotification = "notification.quota";
        public const string AuthorizeNode = "auth.node_authorize";
        public const string UserLogin = "notification.auth.login";
        public const string UserLoginForNode = "auth.node_user_authorize";
        public const string UserRegister = "notification.auth.register";
        /// <summary>
        /// Whenever perissions are set
        /// </summary>
        public const string PermissionsSet = "notification.set.permissions";
        /// <summary>
        /// Whenever an integration is created
        /// </summary>
        public const string IntegrationCreated = "notification.integration.created";
        /// <summary>
        /// Whenever the stage of a model is updated
        /// </summary>
        public const string ModelStageUpdate = "notification.model.stage_update";
        /// <summary>
        /// Whenever an integration is viewed
        /// </summary>
        public const string IntegrationViewed = "notification.integration.view";
        /// <summary>
        /// Whenever a model is edited.
        /// </summary>
        public const string ModelEdit = "notification.model.edit";

        public const string ModelCreate = "notification.model.created";
        public const string ModelBuild = "notification.model.build";
        public const string QuotaUpdate = "notification.quota.sync";

    }
}
