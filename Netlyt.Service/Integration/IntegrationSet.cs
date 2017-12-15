using System.Collections.Generic;
using System.Dynamic;
using Netlyt.Service.IntegrationSource;
using Netlyt.Service.Source;

namespace Netlyt.Service.Integration
{
    /// <summary>
    /// A integration set with definition and data source
    /// </summary>
    public class IntegrationSet 
    {
        public IIntegrationTypeDefinition Definition { get; set; }
        public InputSource Source { get; set; }

        public IntegrationSet(IIntegrationTypeDefinition inputDef, InputSource source)
        {
            this.Definition = inputDef;
            this.Source = source;
            //Definition.Save();
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

        public IEnumerable<IntegratedDocument> AsEnumerable()
        {
            dynamic nextItem;
            while ((nextItem = Read()) != null)
            {
                yield return nextItem;
            }
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
        /// <summary>
        /// Wraps the data in an integration document
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public IntegratedDocument Wrap(ExpandoObject data)
        {
            return Definition.Wrap(data);
        }
    }
}