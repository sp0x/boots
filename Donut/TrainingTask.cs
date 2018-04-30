using System.ComponentModel.DataAnnotations.Schema;
using Donut.Models;
using Netlyt.Interfaces;
namespace Donut
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
