using System.ComponentModel.DataAnnotations.Schema;

namespace Donut.Models
{
    public class TrainingScript
    {
        public long Id { get; set; }
        public string DonutScript { get; set; }
        public string PythonScript { get; set; }
        [ForeignKey("TrainingTask")]
        public long TrainingTaskId { get; set; }
        public virtual TrainingTask TrainingTask { get; set; }

        public TrainingScript()
        {

        }
        public TrainingScript(string donut, string py)
        {
            this.DonutScript = donut;
            this.PythonScript = py;
        }
    }
}