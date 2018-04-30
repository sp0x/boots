using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Donut.Integration;
using Netlyt.Interfaces;

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

        public double Progress
        {
            get
            {
                return 100 * ((double)Formatter.Position() / Math.Max(1, Size));
            }
        }

        public void SetFormatter(IInputFormatter formatter) 
        {
            this.Formatter = formatter;
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
        protected DataIntegration CreateIntegrationFromObj(dynamic firstInstance, string name)
        {
            DataIntegration typeDef = null;
            if (firstInstance != null)
            {
                typeDef = new DataIntegration();
                typeDef.DataEncoding = Encoding.CodePage;
                typeDef.DataFormatType = Formatter.Name;
                typeDef.SetFieldsFromType(firstInstance);
            }
            return typeDef;
        }
    }
}