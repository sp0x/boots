using System;

namespace Netlyt.Interfaces
{
    public class SourceFromIntegration : Attribute
    {
        public string IntegrationName { get; set; }
        public SourceFromIntegration(string integerationName)
        {
            IntegrationName = integerationName;
        }
    }
}