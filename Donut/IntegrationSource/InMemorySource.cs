using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Donut.Integration;
using Netlyt.Interfaces;

namespace Donut.IntegrationSource
{
    public class InMemorySource : InputSource
    { 
        public Stream Content { get; private set; } 
        private object _lock = new object();
        private dynamic _cachedInstance;
        private DataIntegration _cachedIntegration;

        public InMemorySource(string content) : base()
        {
            this.Content = new MemoryStream(Encoding.GetBytes(content)); 
        }

        public InMemorySource(Stream stream) : base()
        { 
            this.Content = stream;
        }

        /// <summary>
        /// Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static InMemorySource Create<T>(string payload, IInputFormatter<T> formatter = null)
            where T : class
        {
            var src = new InMemorySource(payload);
            src.SetFormatter(formatter);
            return src;
        }

        /// <summary>
        /// Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static InMemorySource Create<T>(Stream payload, IInputFormatter<T> formatter = null)
            where T : class
        {
            var src = new InMemorySource(payload);
            src.SetFormatter(formatter);
            return src;
        }

        /// <summary>
        /// </summary>
        /// <inheritdoc/>
        /// <returns>The input files as source</returns>
        public override IEnumerable<IInputSource> Shards()
        {
            var source = new InMemorySource(Content);
            source.SetFormatter(Formatter);
            source._cachedInstance = _cachedInstance;
            yield return source;
        }

        public override IIntegration ResolveIntegrationDefinition()
        {
            if (_cachedIntegration != null) return _cachedIntegration;
            var iterator = Formatter.GetIterator(Content, true);
            var firstInstance = _cachedInstance = iterator.First();
            var outputIntegration = CreateIntegrationFromObj(firstInstance, null);
            return _cachedIntegration = outputIntegration;
        }

        public override IEnumerable<T> GetIterator<T>()
        {
            lock (_lock)
            {
                IEnumerable<T> iterator = null;
                var resetNeeded = _cachedInstance != null;
                //Probably throw?
                if (resetNeeded && Content.CanSeek)
                {
                    Content.Position = 0;
                    _cachedInstance = null;
                }
                iterator = ((IInputFormatter<T>) Formatter).GetIterator(Content, resetNeeded);
                return iterator;
            }
        }
         
        /// <summary>
        /// Gets the next object instance
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<dynamic> GetIterator(Type targetType=null)
        {
            lock (_lock)
            {
                IEnumerable<dynamic> iterator = null;
                var resetNeeded = _cachedInstance != null;
                //Probably throw?
                if (resetNeeded && Content.CanSeek)
                {
                    Content.Position = 0;
                    _cachedInstance = null;
                }
                iterator = Formatter.GetIterator(Content, resetNeeded);
                return iterator;
            }
        }

        public override void DoDispose()
        { 
            Content?.Dispose(); 
        }
 
        
    }
}