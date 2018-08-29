using System.Collections.Generic;

namespace Netlyt.Data.ViewModels
{
    public class ModelBuildViewModel
    {
        public long Id { get; set; }
        public string TaskType { get; set; }
        public string Scoring { get; set; }
        public string CurrentModel { get; set; }
        public string Endpoint { get; set; }
        public string Target { get; set; }
        public ModelTrainingPerformanceViewModel Performance { get; set; }
        public IEnumerable<PermissionViewModel> Permissions { get; set; }
    }
}