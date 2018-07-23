using System;
using System.ComponentModel.DataAnnotations.Schema;
using Donut.Data;
using Netlyt.Interfaces;

namespace Donut.Models
{
    public class TrainingTask
    {
        public long Id { get; set; }
        //public string OrionTaskId { get; set; }
        [ForeignKey("Model")]
        public long ModelId { get; set; }
        public string Scoring { get; set; }
        [ForeignKey("Target")]
        public long? TargetId { get; set; }
        public virtual TrainingScript Script { get; set; }
        public virtual ModelTarget Target { get; set; }
        public virtual ModelTrainingPerformance Performance { get; set; }
        public int TrainingTargetId { get; set; }
        //public string TargetName { get; set; }
        public virtual Model Model { get; set; }
        public TrainingTaskStatus Status { get; set; }
        public string TypeInfo { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
