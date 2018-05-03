using Netlyt.Interfaces;

namespace Donut.Lex.Generators
{
    public class FeatureFunctionsCodeResult
    {
        public string Content { get; set; }
        public string GroupFields { get; set; }
        public string GroupKeys { get; set; }
        public string Projections { get; set; }

        public FeatureFunctionsCodeResult(string content) : this(DonutFunctionType.Standard, content)
        {

        }
        public FeatureFunctionsCodeResult(DonutFunctionType functionType, string content)
        {
            if (functionType == DonutFunctionType.GroupField)
            {
                GroupFields = content;
            }
            else if (functionType == DonutFunctionType.Project)
            {
                Projections = content;
            }
            else if (functionType == DonutFunctionType.GroupKey)
            {
                GroupKeys = content;
            }
            else
            {
                this.Content = content;
            }
        }

        public string GetValue()
        {
            if (!string.IsNullOrEmpty(Content)) return Content;
            if (!string.IsNullOrEmpty(GroupFields)) return GroupFields;
            if (!string.IsNullOrEmpty(Projections)) return Projections;
            if (!string.IsNullOrEmpty(GroupKeys)) return GroupKeys;
            return null;
        }
    }
}