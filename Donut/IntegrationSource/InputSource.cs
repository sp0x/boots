using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Donut.Data.Format;
using Donut.Encoding;
using Donut.Integration;
using Donut.Source;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;

namespace Donut.IntegrationSource
{
    /// <summary>   An input source. </summary>
    ///
    /// <remarks>   Vasko, 13-Dec-17. </remarks>

    public abstract class InputSource : IInputSource
    {
        
        public long Size { protected set; get; }
        private bool _disposed;  
        public System.Text.Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
        public IInputFormatter Formatter { get; protected set; }
        public bool SupportsSeeking { get; set; }
        public Dictionary<string, FieldOptionsBuilder> FieldOptions { get; set; }
    
        public InputSource()
        {
            FieldOptions = new Dictionary<string, FieldOptionsBuilder>();
        }
        public FieldOptionsBuilder Field(string name)
        {
            if (FieldOptions.ContainsKey(name)) return FieldOptions[name];
            FieldOptions[name] = new FieldOptionsBuilder(this);
            return FieldOptions[name];
        }

        public double Progress
        {
            get
            {
                var pgs = 100 * ((double) Formatter.Position() / Math.Max(1, Size));
                return Math.Max(0,pgs);
            }
        }


        protected virtual IInputSource Clone()
        {
            var type = this.GetType();
            var ctor = type.GetConstructor(new Type[] { });
            var instance = ctor.Invoke(new object[] { }) as InputSource;
            instance.FieldOptions = FieldOptions;
            instance.Formatter = Formatter;
            instance.SupportsSeeking = this.SupportsSeeking;
            instance.Encoding = Encoding;
            instance.Size = Size;
            instance._disposed = _disposed;
            return instance;
        }

        public void SetFormatter(IInputFormatter formatter) 
        {
            this.Formatter = formatter;
            if (formatter != null)
            {
                formatter.SetFieldOptions(FieldOptions);
            }
        }
//        public InputSource(IInputFormatter formatter)
//        {
//            this.Formatter = formatter;
//        }

        public abstract IIntegration ResolveIntegrationDefinition();
        public abstract IEnumerable<dynamic> GetIterator(Type targetType=null);

        public abstract IEnumerable<T> GetIterator<T>()
            where T : class;

        public virtual void DoDispose()
        {
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Formatter.Dispose();
                DoDispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~InputSource()
        {
            Dispose(false);
        }

        public IEnumerable<dynamic> AsEnumerable()
        {
            return GetIterator();
//            dynamic nextItem;
//            while ((nextItem = GetNext()) != null)
//            {
//                yield return nextItem;
//            }
        } 
        public IEnumerator<object> GetEnumerator()
        {
            return AsEnumerable().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal IEnumerable<T> GetIterator<T>(Stream content, bool resetNeeded) where T : class
        {
            var iterator = ((IInputFormatter<T>) Formatter).GetIterator(content, resetNeeded);
            return GetIterator(iterator);
        }

        internal IEnumerable<T> GetIterator<T>(IEnumerable<T> iter) where T : class
        {
            Formatter.SetFieldOptions(FieldOptions);
            return iter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Each part of the input segments if this source has multiple inputs.</returns>
        public virtual IEnumerable<IInputSource> Shards()
        {
            return new List<InputSource>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Each input of the input segment, if this source has multiple inputs.</returns>
        public virtual IEnumerable<dynamic> ShardKeys()
        {
            return new List<dynamic>();
        }

        public static TransformBlock<InputSource, IEnumerable<dynamic>> GetBlock()
        {
            var actionBlock = new TransformBlock<InputSource, IEnumerable<dynamic>>((f) => f.AsEnumerable());
            return actionBlock;
        }

        public virtual void Cleanup()
        { 
        }

        public virtual void Reset()
        {
            Formatter.Reset();
        }

        /// <summary>
        /// Create a new integration from an object instance
        /// </summary>
        /// <param name="firstInstance"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected Data.DataIntegration CreateIntegrationFromObj(dynamic firstInstance, string name)
        {
            Data.DataIntegration typeDef = null;
            if (firstInstance != null)
            {
                typeDef = new Data.DataIntegration();
                typeDef.PublicKey = ApiAuth.Generate();
                typeDef.DataEncoding = Encoding.CodePage;
                typeDef.DataFormatType = Formatter.Name;
                typeDef.SetFieldsFromType(firstInstance);
            }
            //Apply field options
            foreach (var fieldOpPair in FieldOptions)
            {
                FieldDefinition targetField = typeDef.Fields.FirstOrDefault(x => x.Name == fieldOpPair.Key);
                var fieldOp = fieldOpPair.Value;
                if (targetField == null)
                {
                    if (fieldOp.ValueEvaluater != null)
                    {
                        var targetType = fieldOp.ValueEvaluater.ReturnType;
                        var virtField = targetField = new FieldDefinition(fieldOpPair.Key, targetType);
                        virtField.Extras = new FieldExtras() {IsFake = true};
                        typeDef.Fields.Add(virtField);
                    }
                    else
                    {
                        continue; //Maybe throw?
                    }
                }
                if (fieldOp.IgnoreField)
                {
                    typeDef.Fields.Remove(targetField);
                    continue;
                }
                var ops = fieldOp;
                var stringName = typeof(String).Name;
                if (ops.IsString) targetField.Type = stringName;
                if (ops.Encoding != null)
                {
                    FieldEncoding.SetEncoding(typeDef, targetField, ops.Encoding);
                }
                if (targetField.Type == stringName && targetField.Extras ==null)
                {
                    targetField.DataEncoding = FieldDataEncoding.BinaryIntId;
                    targetField.Extras = new FieldExtras();
                    //targetField.Extras.Field = targetField;
                }

                if (ops.Duplicates.Count>0)
                {
                    foreach (var duplicate in ops.Duplicates)
                    {
                        var dupField = new FieldDefinition(duplicate, targetField.Type);
                        typeDef.Fields.Add(dupField);
                    }
                }
            }
            return typeDef;
        }

    }
}