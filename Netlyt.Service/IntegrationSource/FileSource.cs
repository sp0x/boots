using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Source;

namespace Netlyt.Service.IntegrationSource
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

        public FileSource() : base(null)
        {
            _lock = new object();
        }

        public FileSource(string file, IInputFormatter formatter) : base(formatter)
        {
            _lock = new object();
            Path = file;
            Formatter = formatter;
        }

        public FileSource(Stream fileStream, IInputFormatter formatter) : this()
        {
            _fileStream = fileStream;
            Formatter = formatter;
        }

        public FileSource(FileStream fileStream, IInputFormatter formatter) : this((Stream) fileStream, formatter)
        {
            Path = fileStream.Name;
            _fileStream = fileStream;
            Formatter = formatter;
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
        public override IIntegrationTypeDefinition GetTypeDefinition()
        {
            try
            {
                using (var fStream = Open())
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
            var src = new FileSource(fileName, formatter);
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
            var src = new FileSource(fileName, formatter);
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
            var src = new FileSource(stream, formatter);
            return src;
        }

        /// <summary>
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
        ///     Gets the next instance
        /// </summary>
        /// <returns></returns>
        public override dynamic GetNext()
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
                lastInstance = Formatter.GetNext(_fileStream, resetNeeded);
                //If there are no more records in the current file source, and we're using a whole directory as a source
                //and we have any remaining files to check
                if (lastInstance == null && Mode == FileSourceMode.Directory && _fileIndex < _filesCache.Length - 1)
                {
                    _fileIndex++;
                    _fileStream.Close();
                    //We reset, because the stream changed
                    lastInstance = Formatter.GetNext(Open(), true);
                } 
                return lastInstance;
            }
        }

        /// <summary>
        /// </summary>
        /// <inheritdoc/>
        /// <returns>The input files as source</returns>
        public override IEnumerable<InputSource> Shards()
        {
            if (Mode == FileSourceMode.File)
            {
                var inputSource = new FileSource(Path, Formatter);
                yield return inputSource;
            }
            else if (Mode == FileSourceMode.Directory)
            {
                var cache = GetFilenames();
                if (_fileIndex < cache.Length)
                    for (var i = _fileIndex; i < cache.Length; i++)
                    {
                        var file = cache[i];
                        var source = new FileSource(file, Formatter.Clone());
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