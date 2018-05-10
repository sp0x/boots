using System;
using Donut.Encoding;
using Netlyt.Interfaces;

namespace Donut.Data.Format
{
    public class FieldOptionsBuilder
    {
        private IInputSource _src;
        private bool _asString;
        private Type _encoding;
        public bool IsString => _asString;
        public Type Encoding => _encoding;
        public bool IgnoreField { get; set; }

        public FieldOptionsBuilder(IInputSource src)
        {
            _src = src;
        }

        public void TreatAsString()
        {
            _asString = true;
        }

        public void Ignore()
        {
            this.IgnoreField = true;
        }
        public void EncodeWith<T>()
         where T : FieldEncoding
        {
            _encoding = typeof(T);
        }
    }
}