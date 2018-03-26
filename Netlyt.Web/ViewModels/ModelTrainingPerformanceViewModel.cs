using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netlyt.Web.ViewModels
{
    public class ModelTrainingPerformanceViewModel
    {
        public long Id { get; set; }  
        public DateTime TrainedTs { get; set; }
        public double Accuracy { get; set; }
        public string FeatureImportance { get; set; } 
        public string ReportUrl { get; set; } 
        public string TestResultsUrl { get; set; }
        public bool IsRegression { get; set; }
    }
}
