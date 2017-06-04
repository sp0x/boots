using Peeralize.Service.IntegrationSource;
using Peeralize.Service.Source;

namespace Peeralize.Service.Integration
{
    public class IntegrationSet 
    {
        public IntegrationTypeDefinition Definition { get; set; }
        public IInputSource Source { get; set; }

        public IntegrationSet(IntegrationTypeDefinition inputDef, IInputSource source)
        {
            this.Definition = inputDef;
            this.Source = source;
            Definition.Save();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() == typeof(IntegrationSet))
            {
                return Definition.Equals((obj as IntegrationSet).Definition) &&
                       Source.Equals((obj as IntegrationSet).Source);
            }
            else return obj.Equals(this);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Reads the next available item from this set
        /// </summary>
        /// <returns></returns>
        public IntegratedDocument Read()
        {
            var entry = Source.GetNext();
            if (entry == null) return null;
            var doc = new IntegratedDocument();
            doc.SetDocument(entry);
            doc.TypeId = Definition.Id;
            return doc;
        }
    }
}