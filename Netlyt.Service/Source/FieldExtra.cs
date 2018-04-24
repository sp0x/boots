using Netlyt.Interfaces;

namespace Netlyt.Service.Source
{
    public class FieldExtra : IFieldExtra
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public IFieldDefinition Field { get; set; }
        public FieldExtraType Type { get; set; }

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