using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Donut.Data.Format;
using Donut.Integration;
using Netlyt.Interfaces;

namespace Donut.IntegrationSource
{
    /// <summary>
    ///     A source file or collection of files
    /// </summary>
    public class FileSource : InputSource
    {
        private dynamic _cachedInstance;

        //reserved for directory mode
        private int _fileIndex;

        private string[] _filesCache;
        private Stream _fileStream;
        private readonly object _lock;

        public FileSource()
        {
            _lock = new object();
        }

        public FileSource(string file) : base()
        {
            _lock = new object();
            Path = file;
        }

        public FileSource(Stream fileStream) : this()
        {
            _fileStream = fileStream;
        }

        public FileSource(FileStream fileStream) : this((Stream) fileStream)
        {
            Path = fileStream.Name;
            _fileStream = fileStream;
        }

        public bool IsOpen => _fileStream != null && (_fileStream.CanRead || _fileStream.CanWrite);
        public string FileName => System.IO.Path.GetFileNameWithoutExtension(Path);

        /// <summary>
        ///     The initial path that was used for this source.
        /// </summary>
        public string Path { get; }

        public string CurrentPath { get; private set; }

        public FileSourceMode Mode { get; set; }

        /// <summary>
        ///     Gets the type definition of this source.
        /// </summary>
        /// <returns></returns>
        public override IIntegration ResolveIntegrationDefinition()
        {
            try
            {
                using (var fStream = Open())
                {
                    var firstInstance = _cachedInstance = Formatter.GetIterator(fStream, true).FirstOrDefault();
                    var typedef = CreateIntegrationFromObj(firstInstance, FileName);
                    return typedef;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Trace.WriteLine(ex.Message);
            }
            return null;
        }

        /// <summary>
        ///     Opens a stream to the file source
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public Stream Open(FileMode mode = FileMode.Open)
        {
            lock (_lock)
            {
                if (Mode == FileSourceMode.File)
                {
                    if (IsOpen) return _fileStream;
                    _cachedInstance = null;
                    return _fileStream = File.Open(Path, mode, FileAccess.Read, FileShare.Read);
                }
                if (Mode == FileSourceMode.Directory)
                {
                    if (IsOpen) return _fileStream;
                    //Don`t refresh the directory
                    if (_filesCache == null || _filesCache.Length == 0)
                        _filesCache = GetFilenames();
                    if (_fileIndex >= _filesCache.Length) return null;
                    CurrentPath = _filesCache[_fileIndex];
                    _cachedInstance = null;
                    return _fileStream = File.Open(CurrentPath, mode);
                }
                return null;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private string[] GetFilenames()
        {
            var flAttributes = File.GetAttributes(Path);
            //TODO: Optimize this, for large directories
            string[] cache;
            if (flAttributes == FileAttributes.Directory)
                cache = Directory.GetFiles(Path, "*", SearchOption.TopDirectoryOnly);
            else
                cache = Directory.GetFiles(System.IO.Path.GetDirectoryName(Path), FileName,
                    SearchOption.TopDirectoryOnly);
            return cache;
        }

        /// <summary>
        ///     Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static FileSource CreateFromFile(string fileName, IInputFormatter formatter = null) 
        {
            var src = new FileSource(fileName);
            src.SetFormatter(formatter);
            return src;
        }

        /// <summary>
        ///     Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static FileSource CreateFromDirectory(string fileName, IInputFormatter formatter = null)
        {
            var src = new FileSource(fileName);
            src.SetFormatter(formatter);
            src.Mode = FileSourceMode.Directory;
            return src;
        }

        /// <summary>
        ///     Creates a new filesource
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static FileSource Create(Stream stream, IInputFormatter formatter = null)
        {
            var src = new FileSource(stream);
            src.SetFormatter(formatter);
            return src;
        }

        /// <summary>
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public static FileSource Create<T>(FileStream fs, JsonFormatter<T> formatter = null)
            where T : class
        {
            if (fs == null) throw new ArgumentNullException(nameof(fs));
            var src = new FileSource(fs);
            src.SetFormatter(formatter);
            return src;
        }

        public override IEnumerable<T> GetIterator<T>()
        {
            return GetIterator(typeof(T)).Cast<T>();
        }

        /// <summary>
        ///     Gets the next instance
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<dynamic> GetIterator(Type targetType = null)
        {
            lock (_lock)
            {
                dynamic lastInstance = null;
                //If there was a previous run and there's cache open but the stream is not open, reset !
                var resetNeeded = _cachedInstance != null && !IsOpen || !IsOpen;
                if (resetNeeded)
                {
                    Open();
                    _cachedInstance = null;
                }
                //The stream position is increased, so there's no need for anything else.
                lastInstance = Formatter.GetIterator(_fileStream, resetNeeded);
                //If there are no more records in the current file source, and we're using a whole directory as a source
                //and we have any remaining files to check
                if (lastInstance == null && Mode == FileSourceMode.Directory && _fileIndex < _filesCache.Length - 1)
                {
                    _fileIndex++;
                    _fileStream.Close();
                    //We reset, because the stream changed
                    lastInstance = Formatter.GetIterator(Open(), true);
                } 
                return lastInstance;
            }
        }

        /// <summary>
        /// </summary>
        /// <inheritdoc/>
        /// <returns>The input files as source</returns>
        public override IEnumerable<IInputSource> Shards()
        {
            if (Mode == FileSourceMode.File)
            {
                var inputSource = new FileSource(Path);
                inputSource.SetFormatter(Formatter);
                yield return inputSource;
            }
            else if (Mode == FileSourceMode.Directory)
            {
                var cache = GetFilenames();
                if (_fileIndex < cache.Length)
                    for (var i = _fileIndex; i < cache.Length; i++)
                    {
                        var file = cache[i];
                        var source = new FileSource(file);
                        source.SetFormatter(Formatter.Clone());
                        source.Encoding = Encoding;
                        yield return source;
                    }
            }
        }

        /// <summary>
        /// </summary>
        /// <inheritdoc/>
        /// <returns>The input filenames</returns>
        public override IEnumerable<dynamic> ShardKeys()
        {
            if (Mode == FileSourceMode.File)
            {
                yield return Path;
            }
            else if (Mode == FileSourceMode.Directory)
            {
                var cache = GetFilenames();
                if (_fileIndex < cache.Length)
                    for (var i = _fileIndex; i < cache.Length; i++)
                    {
                        var file = cache[i];
                        yield return file;
                    }
            }
        }



        public override void DoDispose()
        {
            _fileStream?.Dispose();
        }

        public override string ToString()
        {
            return FileName;
        }

    }
}