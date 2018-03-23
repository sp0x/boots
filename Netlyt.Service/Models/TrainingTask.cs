using System.ComponentModel.DataAnnotations.Schema;
using Netlyt.Service.Ml;

namespace Netlyt.Service.Models
{
    public class TrainingTask
    {
        public long Id { get; set; }
        //public string OrionTaskId { get; set; }
        [ForeignKey("Model")]
        public long ModelId { get; set; }
        public Model Model { get; set; } 
        public TrainingTaskStatus Status { get; set; }
    }
}