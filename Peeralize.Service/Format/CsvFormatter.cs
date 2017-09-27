
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using Peeralize.Service.Source;

namespace Peeralize.Service.Format
{
    public class CsvFormatter : IInputFormatter, IDisposable
    {
        public string Name => "Csv";
        public char Delimiter { get; set; } = ';';
        private string[] _headers;
        private StreamReader _reader;
        private CsvReader _csvReader;

        public CsvFormatter()
        {

        }

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
            if (_headers == null || reset)
            {
                _headers = _csvReader.GetFieldHeaders();
            }

            var outputObject = new ExpandoObject() as IDictionary<string, Object>;
            if (_csvReader.ReadNextRecord())
            {
                for (var i = 0; i < _csvReader.FieldCount; i++)
                {
                    string fldValue = _csvReader[i];
                    string fldName = i >= _headers.Length ? ("NoName_" + i) : _headers[i];
                    //Ignore invalid columns
                    if (fldName == $"Column{i}" && string.IsNullOrEmpty(fldValue))
                    {
                        continue;
                    }
                    double dValue;
                    fldValue = fldValue.Trim('"', '\t', '\n', '\r', '\'');
                    if (double.TryParse(fldValue, out dValue))
                    {
                        if (fldValue.Contains(".") || fldValue.Contains(","))
                        {
                            outputObject.Add(fldName, dValue);
                        }
                        else
                        {
                            outputObject.Add(fldName, (long)dValue);
                        }
                    }
                    else
                    {
                        outputObject.Add(fldName, fldValue);
                    }
                }
            }
            else
            {
                return default(T);
            }
            return outputObject as T;
        }

        public IInputFormatter Clone()
        {
            var formatter = new CsvFormatter();
            formatter.Delimiter = this.Delimiter;
            return formatter;
        }

        public double Progress => 0;


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (!_csvReader.IsDisposed)
                {
                    _csvReader.Dispose();
                }
                _csvReader = null;
                _reader.Dispose();
                _disposed = true;
            }
        }

        ~CsvFormatter()
        {
            Dispose(false);
        }
    }
}