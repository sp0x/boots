using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using nvoid.db.DB;

namespace Netlyt.Service.Ml
{
    public class ModelTrainingPerformance : Entity
    {
        public long Id { get; set; }
        [ForeignKey("Model")]
        public long ModelId { get; set; }
        public Model Model { get; set; }
        public DateTime TrainedTs { get; set; }
        public double Accuracy { get; set; }
        public string FeatureImportance { get; set; }
        [Column(TypeName = "VARCHAR")]
        [StringLength(255)]
        public string ReportUrl { get; set; }
        [Column(TypeName = "VARCHAR")]
        [StringLength(255)]
        public string TestResultsUrl { get; set; }
    }
}