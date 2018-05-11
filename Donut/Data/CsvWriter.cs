using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Donut.Data
{
    public class CsvWriter
    {
        private FileStream _fs;
        private StreamWriter _writer;

        public CsvWriter(string file)
        {
            _fs = System.IO.File.Open(file, FileMode.OpenOrCreate, FileAccess.Write);
            _writer = new StreamWriter(_fs);
        }

        public void WriteLine(params string[] elements)
        {
            for (var i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                var hasQ = element.Contains("\"");
                var hasC = element.Contains(",");
                if(hasQ || hasC) _writer.Write("\"");
                _writer.Write(element);
                if(hasQ || hasC) _writer.Write("\"");
                if (i < (elements.Length-1))
                {
                    _writer.Write(",");
                }
            }
            _writer.WriteLine();
        }
    }
}
