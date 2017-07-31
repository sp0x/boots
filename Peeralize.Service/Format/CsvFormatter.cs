
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using Peeralize.Service.Source;

namespace Peeralize.Service.Format
{
    public class CsvFormatter : IInputFormatter
    {
        public string Name => "Csv";
        public char Delimiter { get; set; } = ';';
        private StreamReader _reader;
        private CsvReader _csvReader;
        /// <summary>
        /// Gets the next record in an Expando object
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="reset"></param>
        /// <returns></returns>
        public dynamic GetNext(Stream fs, bool reset = false)
        {
            return GetNext<ExpandoObject>(fs, reset);
        }

        public T GetNext<T>(Stream fs, bool reset = false)
            where T : class
        {
            if (!fs.CanRead)
            {
                return default(T);
            }
            _reader = (!reset && _reader != null) ? _reader : new StreamReader(fs);
            _csvReader = (!reset && _csvReader != null) ? _csvReader : new CsvReader(_reader, true, Delimiter);

            var headers = _csvReader.GetFieldHeaders(); 
            var outputObject = new ExpandoObject() as IDictionary<string, Object>;
            if (_csvReader.ReadNextRecord())
            {
                for (var i = 0; i < _csvReader.FieldCount; i++)
                {
                    string fldValue = _csvReader[i];
                    string fldName = i>= headers.Length ? ("NoName_" + i) : headers[i];
                    double dValue;
                    if (double.TryParse(fldValue, out dValue))
                    {
                        if (fldValue.Contains(".") || fldValue.Contains(","))
                        {
                            outputObject.Add(fldName, dValue);
                        }
                        else
                        {
                            outputObject.Add(fldName, (long) dValue);
                        }
                    }
                    else
                    {
                        outputObject.Add(fldName, fldValue);
                    }
                }
                return outputObject as T;
            }
            return default(T);
        }
    }
}