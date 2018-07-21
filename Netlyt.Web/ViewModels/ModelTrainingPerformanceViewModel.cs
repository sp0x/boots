using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Netlyt.Web.ViewModels
{
    public class ModelTrainingPerformanceViewModel
    {
        public long Id { get; set; }  
        public DateTime TrainedTs { get; set; }
        public string TargetName { get; set; }
        public double Accuracy { get; set; }
        public string FeatureImportance { get; set; } 
        public string AdvancedReport { get; set; }
        public JObject MontlyUsage { get; set; }
        public JObject WeeklyUsage { get; set; }
        public string LastRequestIP { get; set; }
        public DateTime LastRequestTs { get; set; }
        public string ReportUrl { get; set; } 
        public string TestResultsUrl { get; set; }
        public bool IsRegression { get; set; }
        public string TaskType { get; set; }
        public string ModelName { get; set; }
    }
}
