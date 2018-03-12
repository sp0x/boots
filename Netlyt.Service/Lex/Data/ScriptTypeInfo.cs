namespace Netlyt.Service.Lex.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class ScriptTypeInfo
    {
        public string Name { get; set; }

        public string GetClassName()
        {
            return Name;
        }

        public string GetContextName()
        {
            var name = GetClassName();
            return $"{name}Context";
        }
    }
}