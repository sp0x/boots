using System.Collections.Generic;

namespace Peeralize.Service.Source
{
    public class FieldExtras
    {
        public Dictionary<string, string> Extra { get; set; } 
        public bool Unique { get; set; }
        public bool Nullable { get; set; }
    }
}