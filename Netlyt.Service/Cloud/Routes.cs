namespace Netlyt.Service.Cloud
{
    public class Routes
    {
        public const string MessageNotification = "notification.message";
        /// <summary>
        /// Whenever a user logins
        /// </summary>
        public const string UserLoginNotification = "notification.auth.login";
        public const string UserLoginForNode = "auth.node_user_authorize";
        /// <summary>
        /// Whenever a user registers
        /// </summary>
        public const string UserRegisterNotification = "notification.auth.register";
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

        public const string AuthorizeNode = "auth.node_authorize";
        public const string AuthorizationResponse = "auth.node_authorize.response";

    }
}
