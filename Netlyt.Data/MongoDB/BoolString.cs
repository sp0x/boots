namespace Netlyt.Data.MongoDB
{
    public class BoolString
    {
        public string Value { get; set; }
        public bool Bool { get; set; }
        public BoolString(string str, bool bval)
        {
            Value = str;
            Bool = bval;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}