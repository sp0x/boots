namespace Donut
{
    /// <summary>
    /// 
    /// </summary>
    public class ScriptTypeInfo
    {
        public string Name { get; set; }

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