using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Donut.Models;
using Donut.Source;

namespace Donut.Data
{
    public class ModelTargets
    {
        public virtual Model Model { get; set; }
        public virtual ICollection<FieldDefinition> Columns { get; set; }
        public virtual ICollection<TargetConstraint> Constraints { get; set; }
        
        public ModelTargets()
        {
            Columns = new List<FieldDefinition>();
            Constraints = new List<TargetConstraint>();
        }

        public ModelTargets AddTarget(FieldDefinition field)
        {
            this.Columns.Add(field);
            return this;
        }

        public bool Has(string argName)
        {
            return Columns.Any(x => x.Name == argName);
        }

        public string ToDonutScript()
        {
            var columnsSb = new StringBuilder();
            var constraints = new StringBuilder();
            columnsSb.Append("targets(");
            columnsSb.Append(String.Join(",", Columns.Select(x => x.Name).ToArray()));
            columnsSb.Append(")");
            return columnsSb.ToString() + constraints.ToString();
        }
    }
}