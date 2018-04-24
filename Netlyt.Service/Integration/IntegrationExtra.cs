using System.Collections.Generic;
using Netlyt.Interfaces;

namespace Netlyt.Service.Integration
{
    public class IntegrationExtra : IIntegrationExtra
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}