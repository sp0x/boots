using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Donut.Data.Format
{
    public abstract class InputFormatter<T> : IInputFormatter<T> where T : class
    {
        //private List<KeyValuePair<string, FieldOptionsBuilder>> _duplicateFields;
        public abstract IEnumerable<T> GetIterator(Stream fs, bool reset);
        public abstract void Dispose();
        public string Name { get; set; }
        public abstract void Reset();
        public abstract long Position();
        public abstract IEnumerable<dynamic> GetIterator(Stream fs, bool reset, Type targetType = null);
        public abstract IInputFormatter Clone();
        private Dictionary<string, FieldOptionsBuilder> FieldOptions { get; set; }

        public void SetFieldOptions(Dictionary<string, FieldOptionsBuilder> fieldOptions)
        {
            FieldOptions = fieldOptions;
        }

        protected void FormatFields(IDictionary<string, object> outputObject)
        {
            foreach (var fldPair in FieldOptions)
            {
                var fldOptions = fldPair.Value;
                if (fldOptions.ValueEvaluater != null)
                {
                    outputObject[fldPair.Key] = fldOptions.ValueEvaluaterFunc(outputObject);
                }
                if (fldOptions.Duplicates.Count > 0)
                {
                    foreach (var duplicate in fldOptions.Duplicates)
                    {
                        outputObject[duplicate] = outputObject[fldPair.Key];
                    }
                }
            }

        }

        protected string FormatFieldName(string fldName)
        {
            return Cleanup.CleanupFieldName(fldName);
        }
    }
}