using System.ComponentModel.DataAnnotations.Schema;
using Netlyt.Interfaces;

namespace Donut.Models
{
    public class TrainingTask
    {
        public long Id { get; set; }
        //public string OrionTaskId { get; set; }
        [ForeignKey("Model")]
        public long ModelId { get; set; }
        public virtual Model Model { get; set; }
        public TrainingTaskStatus Status { get; set; }
    }
}
