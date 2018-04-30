using System.Linq;

namespace Donut.Build
{
    public class SourceContent
    {
        public string Content { get; set; }
        public string File { get; set; }
        public SourceContent(string src, string file)
        {
            Content = src;
            File = file;
        }

        public static SourceContent[] All(params string[] sources)
        {
            return (sources.Select(x => new SourceContent(x, null))).ToArray();
        }
    }
}