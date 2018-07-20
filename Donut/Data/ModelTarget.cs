using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using Donut.Models;
using Donut.Source;

namespace Donut.Data
{
    public class ModelTarget
    {
        public long Id { get; set; }
        [ForeignKey("Model")]
        public long ModelId { get; set; }
        public virtual Model Model { get; set; }
        public virtual FieldDefinition Column { get; set; }
        public virtual ICollection<TargetConstraint> Constraints { get; set; }
        
        public ModelTarget(FieldDefinition fdef = null)
        {
            Constraints = new List<TargetConstraint>();
            Column = fdef;
        }
        public ModelTarget()
        {
            Constraints = new List<TargetConstraint>();
        }
        public string ToDonutScript()
        {
            var columnsSb = new StringBuilder();
            var constraints = new StringBuilder();
            columnsSb.Append("targets(");
            columnsSb.Append(Column.Name);
            columnsSb.Append(")");
            return columnsSb.ToString() + constraints.ToString();
        }

    }
}