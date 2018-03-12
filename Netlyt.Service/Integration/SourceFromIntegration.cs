using System;

namespace Netlyt.Service.Integration
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