using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Peeralize.Service.Integration;
using Peeralize.Service.Source;

namespace Peeralize.Service.IntegrationSource
{
    /// <summary>
    /// 
    /// </summary>
    public class FileSource : IInputSource
    {
        private Stream _fileStream;
        private string _filePath;
        /// <summary>
        /// Not supported
        /// </summary>
        public int Size => 0;

        public IInputFormatter Formatter { get; private set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public bool IsOpen => _fileStream != null && (_fileStream.CanRead || _fileStream.CanWrite);
        private object _lock;
        public string FileName => System.IO.Path.GetFileNameWithoutExtension(_filePath);
        public string Path => _filePath;
        private dynamic _cachedInstance = null;

        public FileSource()
        {
            _lock = new object();
            Encoding = System.Text.Encoding.Default;
        }
        public FileSource(string file, IInputFormatter formatter) : this()
        {
            _filePath = file;
            this.Formatter = formatter;
        }
        public FileSource(Stream fileStream, IInputFormatter formatter) : this()
        {
            this._fileStream = fileStream;
            Formatter = formatter;
        }
        public FileSource(FileStream fileStream, IInputFormatter formatter) : this((Stream)fileStream, formatter)
        {
            _filePath = fileStream.Name;
            this._fileStream = fileStream;
            Formatter = formatter;
        }



        /// <summary>
        /// Gets the type definition of this source.
        /// </summary>
        /// <returns></returns>
        public IIntegrationTypeDefinition GetTypeDefinition()
        {
            try
            {
                using (Stream fStream = Open())
                { 
                    var firstInstance = _cachedInstance = Formatter.GetNext(fStream, true);
                    IntegrationTypeDefinition typedef = null;
                    if (firstInstance != null)
                    {
                        typedef = new IntegrationTypeDefinition(FileName);
                        typedef.CodePage = Encoding.CodePage;
                        typedef.OriginType = Formatter.Name;
                        typedef.ResolveFields(firstInstance);
                    }
                    return typedef;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return null;
        }



        /// <summary>
        /// Opens a stream to the file source
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public Stream Open(FileMode mode = FileMode.Open)
        {
            lock (_lock)
            {
                if (IsOpen) return _fileStream;
                _cachedInstance = null;
                return _fileStream = System.IO.File.Open(_filePath, mode);
            }
        }

        /// <summary>
        /// Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static FileSource CreateFromFile(string fileName, IInputFormatter formatter = null)
        {
            var src = new FileSource(fileName, formatter);
            return src;
        }

        /// <summary>
        /// Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static FileSource Create(Stream stream, IInputFormatter formatter = null)
        {
            var src = new FileSource(stream, formatter);
            return src;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static FileSource Create(FileStream fs, JsonFormatter formatter = null)
        {
            if (fs == null) throw new ArgumentNullException(nameof(fs));
            var src = new FileSource(fs, formatter);
            return src;
        }
        /// <summary>
        /// Gets the next instance
        /// </summary>
        /// <returns></returns>
        public dynamic GetNext()
        {
            lock (_lock)
            {
                dynamic lastInstance = null;
                //If there was a previous run and there's cache open but the stream is not open, reset !
                var resetNeeded = _cachedInstance != null && !IsOpen;
                if (resetNeeded)
                {
                    Open();
                    _cachedInstance = null;
                }
                //The stream position is increased, so there's no need for anything else.
                lastInstance = Formatter.GetNext(_fileStream, resetNeeded); 
                return lastInstance;
            }
        }
    }
}
