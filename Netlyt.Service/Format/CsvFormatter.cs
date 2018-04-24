
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using Netlyt.Interfaces;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.Format
{ 
    public class CsvFormatter<T> : IInputFormatter<T>, IDisposable
        where T : class
    {
        public string Name => "Csv";
        public char Delimiter { get; set; } = ';';
        private string[] _headers;
        private StreamReader _reader;
        private CsvReader _csvReader;
        private long _position = -1;
        private bool _usePresetHeaders = false;
        public string[] Headers
        {
            get
            {
                return _headers;
            }
            set
            {
                _headers = value;
                _usePresetHeaders = true;
            }
        }
        public bool SkipHeader { get; set; }

        public CsvFormatter()
        {
            _dateTimeParser = new DateParser();
        }

        /// <summary>
        /// Gets the next record in an Expando object
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="reset"></param>
        /// <returns></returns>
        public IEnumerable<dynamic> GetIterator(Stream fs, bool reset = false, Type targetType = null)
        {
            return GetIterator(fs, reset);
        }

        public IEnumerable<T> GetIterator(Stream fs, bool reset = false)
        {
            if (!fs.CanRead)
            {
                yield break;
            } 
            _reader = (!reset && _reader != null) ? _reader : new StreamReader(fs);
            _csvReader = (!reset && _csvReader != null) ? _csvReader : new CsvReader(_reader, true, Delimiter);
            if (!SkipHeader && (_headers == null || reset))
            {
                var headerValues = _csvReader.GetFieldHeaders();
                if (!_usePresetHeaders) _headers = headerValues;
            }
            while (_csvReader.ReadNextRecord())
            {
                var outputObject = new ExpandoObject() as IDictionary<string, Object>;
                for (var i = 0; i < _csvReader.FieldCount; i++)
                {
                    string fldValue = _csvReader[i];
                    string fldName = i >= _headers.Length ? ("NoName_" + i) : _headers[i];
                    //Ignore invalid columns
                    if (fldName == $"Column{i}" && string.IsNullOrEmpty(fldValue))
                    {
                        continue;
                    }
                    double? dValue;
                    DateTime tmValue;
                    fldValue = fldValue.Trim('"', '\t', '\n', '\r', '\'');
                    bool isDate = _dateTimeParser.TryParse(fldValue, out tmValue, out dValue);
                    if (dValue!=null)
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
                    else if (isDate)
                    {
                        outputObject.Add(fldName, tmValue);
                    }
                    else
                    {
                        outputObject.Add(fldName, fldValue);
                    }
                }

                _position++;
                yield return outputObject as T;
            }
        }

        public IInputFormatter Clone()
        {
            var formatter = new CsvFormatter<T>();
            formatter.Delimiter = this.Delimiter;
            return formatter;
        }

        public void Reset()
        {
            throw new NotImplementedException();
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
        private DateParser _dateTimeParser;

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