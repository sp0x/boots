
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using Netlyt.Service.Source;

namespace Netlyt.Service.Format
{
    public class CsvFormatter 
        : IInputFormatter, IDisposable
    {
        public string Name => "Csv";
        public char Delimiter { get; set; } = ';';
        private string[] _headers;
        private StreamReader _reader;
        private CsvReader _csvReader;
        private long _position = -1;

        public CsvFormatter()
        {

        }

        /// <summary>
        /// Gets the next record in an Expando object
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="reset"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> GetIterator(Stream fs, bool reset = false)
        {
            return GetIterator<ExpandoObject>(fs, reset);
        }

        public IEnumerable<T> GetIterator<T>(Stream fs, bool reset = false)
            where T : class
        {
            if (!fs.CanRead)
            {
                yield break;
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
                yield break;
            }
            _position++;
            yield return outputObject as T;
        }

        public IInputFormatter Clone()
        {
            var formatter = new CsvFormatter();
            formatter.Delimiter = this.Delimiter;
            return formatter;
        }

        public long Position()
        {
            return _position;
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