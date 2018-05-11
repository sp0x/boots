using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Donut.Encoding;
using Netlyt.Interfaces;

namespace Donut.Data.Format
{
    public class FieldOptionsBuilder
    {
        private IInputSource _src;
        private bool _asString;
        private Type _encoding;
        private HashSet<string> _duplicates;
        private Expression<Func<IDictionary<string, object>, object>> _valueEval;
        private Func<IDictionary<string, object>, object> _valueEvalCompiled;
        public bool IsString => _asString;
        public Type Encoding => _encoding;
        public bool IgnoreField { get; set; }
        public HashSet<string> Duplicates => _duplicates;
        public Expression<Func<IDictionary<string, object>, object>> ValueEvaluater => _valueEval;

        public Func<IDictionary<string, object>, object> ValueEvaluaterFunc
        {
            get
            {
                if (_valueEvalCompiled != null) return _valueEvalCompiled;
                _valueEvalCompiled = _valueEval.Compile();
                return _valueEvalCompiled;
            }
        }

        public FieldOptionsBuilder(IInputSource src)
        {
            _src = src;
            _duplicates = new HashSet<string>();
        }

        public FieldOptionsBuilder AsString()
        {
            _asString = true;
            return this;
        }

        public FieldOptionsBuilder Ignore()
        {
            this.IgnoreField = true;
            return this;
        }
        public FieldOptionsBuilder EncodeWith<T>()
         where T : FieldEncoding
        {
            _encoding = typeof(T);
            return this;
        }

        public FieldOptionsBuilder DuplicateAs(string duplicateName)
        {
            _duplicates.Add(duplicateName);
            return this;
        }

        public void SetValue(Expression<Func<IDictionary<string, object>, object>> func)
        {
            _valueEval = func;
            _valueEvalCompiled = func.Compile();
        }
    }
}