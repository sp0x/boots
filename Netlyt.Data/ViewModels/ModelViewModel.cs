using System;
using System.Collections.Generic;

namespace Netlyt.Data.ViewModels
{
    public class ModelCreationViewModel
    {
        public string Name { get; set; }
        public string DataSource { get; set; }
        public string Callback { get; set; }
        public bool GenerateFeatures { get; set; }
        public string[][] Relations { get; set; }
        public string TargetAttribute { get; set; }

        public ModelCreationViewModel()
        {
            Relations = new string[][]{};
        }

    }

    public class ModelUpdateViewModel
    {
        public long Id { get; set; }
        public string ModelName { get; set; }
        public string DataSource { get; set; }
        public string CallbackUrl { get; set; }
    }

    public class ModelBuildViewModel
    {
        public long Id { get; set; }
        public string TaskType { get; set; }
        public string Scoring { get; set; }
        public string CurrentModel { get; set; }
        public string Endpoint { get; set; }
        public string Target { get; set; }
        public ModelTrainingPerformanceViewModel Performance { get; set; }
    }
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

    }
}