namespace Donut
{
    /// <summary>
    /// 
    /// </summary>
    public class ScriptTypeInfo
    {
        public string Name { get; set; }

        public ScriptTypeInfo(string name)
        {
            this.Name = name;
        }

        public string GetClassName()
        {
            var clearedName = Name.Replace('-', '_').Replace('.', '_').Replace(' ', '_').Replace(';', '_');
            return clearedName;
        }

        public string GetContextName()
        {
            var name = GetClassName();
            return $"{name}Context";
        }
    }
}