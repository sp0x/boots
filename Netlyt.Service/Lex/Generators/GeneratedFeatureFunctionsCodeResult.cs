namespace Netlyt.Service.Lex.Generators
{
    public class GeneratedFeatureFunctionsCodeResult
    {
        public string Content { get; set; }
        public string GroupFields { get; set; }
        public string GroupKeys { get; set; }
        public string Projections { get; set; }

        public GeneratedFeatureFunctionsCodeResult(string content)
        {
            this.Content = content;
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