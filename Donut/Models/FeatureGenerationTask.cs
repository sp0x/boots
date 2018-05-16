using System.ComponentModel.DataAnnotations.Schema;

namespace Donut.Models
{
    public class FeatureGenerationTask
    {
        public long Id { get; set; }
        //public string OrionTaskId { get; set; }
        [ForeignKey("Model")]
        public long ModelId { get; set; }
        public virtual Model Model { get; set; }
        public FeatureGenerationTaskStatus Status { get; set; }
    }
}