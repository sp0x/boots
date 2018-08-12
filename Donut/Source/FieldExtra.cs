using System.ComponentModel.DataAnnotations.Schema;
using Netlyt.Interfaces;

namespace Donut.Source
{
    public class FieldExtra : IFieldExtra
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public virtual FieldDefinition Field { get; set; }
        public FieldExtraType Type { get; set; }
        [ForeignKey("FieldExtras")]
        public long FieldExtrasId { get; set; }
        public virtual FieldExtras FieldExtras { get; set; }

        public FieldExtra()
        {

        }

        public FieldExtra(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }
    }


}