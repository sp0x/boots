using System;
using System.Collections.Generic;

namespace Netlyt.Data.ViewModels
{
    public class ModelViewModel
    {
        public long Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModelName { get; set; }
        public string Callback { get; set; }
        public string TrainingParams { get; set; }
        public string HyperParams { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public bool IsBuilding { get; set; }
        public string Status { get; set; }
        public bool UserIsOwner { get; set; }
        public IEnumerable<ModelBuildViewModel> BuiltTargets { get; set; }
        public IEnumerable<PermissionViewModel> Permissions {get; set; }
        public IEnumerable<FieldDefinitionViewModel> Fields { get; set; }

    }
}