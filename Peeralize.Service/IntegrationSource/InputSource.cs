using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    public abstract class InputSource : IInputSource
    {
        
        public int Size { get; }
        private bool _disposed;  
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public IInputFormatter Formatter { get; protected set; }
        public bool SupportsSeeking { get; set; }
        

        public InputSource(IInputFormatter formatter)
        {
            this.Formatter = formatter;
        }

        public abstract IIntegrationTypeDefinition GetTypeDefinition();
        public abstract dynamic GetNext();

        public dynamic GetNext<T>()
            where T : class
        {
            return GetNext() as T;
        }

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
            dynamic nextItem;
            while ((nextItem = GetNext()) != null)
            {
                yield return nextItem;
            }
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
        public virtual IEnumerable<InputSource> Shards()
        {
            while (false)
            {
                yield return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Each input of the input segment, if this source has multiple inputs.</returns>
        public virtual IEnumerable<dynamic> ShardKeys(){
            while(false){
                yield return null;
            }
        }

        public static TransformBlock<InputSource, IEnumerable<dynamic>> GetBlock()
        {
            var actionBlock = new TransformBlock<InputSource, IEnumerable<dynamic>>((f) =>
            {
                return f.AsEnumerable();
            });
            return actionBlock;
        }

        public virtual void Cleanup()
        { 
        }
    }
}