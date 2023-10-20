using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public partial class GistVirtualFiles : AbstractVirtualPathProviderBase, IVirtualFiles
    {
        public IGistGateway Gateway { get; }
        public string GistId { get; private set; }

        private readonly GistVirtualDirectory rootDirectory;

        public GistVirtualFiles(string gistId) : this(gistId, new GitHubGateway()) { }
        public GistVirtualFiles(string gistId, string accessToken) : this(gistId, new GitHubGateway(accessToken)) { }

        public GistVirtualFiles(string gistId, IGistGateway gateway)
        {
            this.Gateway = gateway;
            this.GistId = gistId;
            this.rootDirectory = new GistVirtualDirectory(this, null, null);
        }

        public GistVirtualFiles(Gist gist) : this(gist.Id) => InitGist(gist);

        public GistVirtualFiles(Gist gist, string accessToken) : this(gist.Id, accessToken) => InitGist(gist);
        public GistVirtualFiles(Gist gist, IGistGateway gateway) : this(gist.Id, gateway) => InitGist(gist);

        private void InitGist(Gist gist)
        {
            gistCache = gist;
            LastRefresh = gist.Updated_At.GetValueOrDefault(DateTime.UtcNow);
        }

        public DateTime LastRefresh { get; private set; }
        
        public TimeSpan RefreshAfter { get; set; } = TimeSpan.MaxValue;

        public const char DirSep = '\\';

        public override IVirtualDirectory RootDirectory => rootDirectory;

        public override string VirtualPathSeparator => "/";

        public override string RealPathSeparator => "\\";

        protected override void Initialize() { }

        public static bool IsDirSep(char c) => c == '\\' || c == '/';

        public const string Base64Modifier = "|base64";

        private static byte[] FromBase64String(string path, string base64String)
        {
            try
            {
                if (string.IsNullOrEmpty(base64String))
                    return TypeConstants.EmptyByteArray;
                
                return Convert.FromBase64String(base64String);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Could not convert Base 64 contents of '{path}', length: {base64String.Length}, starting with: {base64String.SafeSubstring(50)}",
                    ex);
            }
        }

        public static bool GetGistTextContents(string filePath, Gist gist, out string text)
        {
            if (GetGistContents(filePath, gist, out text, out var bytesContent))
            {
                if (text == null)
                    text = MemoryProvider.Instance.FromUtf8(bytesContent.GetBufferAsMemory().Span).ToString();
                return true;
            }
            return false;
        }
        
        public static bool GetGistContents(string filePath, Gist gist, out string text, out MemoryStream stream)
        {
            var base64FilePath = filePath + Base64Modifier;
            foreach (var entry in gist.Files)
            {
                var file = entry.Value;
                var isMatch = entry.Key == filePath || entry.Key == base64FilePath;
                if (!isMatch)
                    continue;

                // GitHub can truncate Gist and return partial content
                if ((string.IsNullOrEmpty(file.Content) || file.Content.Length < file.Size) && file.Truncated)
                {
                    file.Content = file.Raw_Url.GetStringFromUrl(
                        requestFilter: req => req.With(c => c.UserAgent = nameof(GitHubGateway)));
                }

                text = file.Content;
                if (entry.Key == filePath)
                {
                    if (filePath.EndsWith(Base64Modifier))
                    {
                        stream = MemoryStreamFactory.GetStream(FromBase64String(entry.Key, text));
                        text = null;
                    }
                    else
                    {
                        var bytesMemory = MemoryProvider.Instance.ToUtf8(text.AsSpan());
                        stream = MemoryProvider.Instance.ToMemoryStream(bytesMemory.Span); 
                    }
                    return true;
                }

                if (entry.Key == base64FilePath)
                {
                    stream = MemoryStreamFactory.GetStream(FromBase64String(entry.Key, text));
                    text = null;
                    return true;
                }
            }

            text = null;
            stream = null;
            return false;
        }

        private Gist gistCache;

        public Gist GetGist(bool refresh = false)
        {
            if (gistCache != null && !refresh)
                return gistCache;

            LastRefresh = DateTime.UtcNow;
            return gistCache = Gateway.GetGist(GistId);
        }

        public async Task<Gist> GetGistAsync(bool refresh = false)
        {
            if (gistCache != null && !refresh)
                return gistCache;

            LastRefresh = DateTime.UtcNow;
            return gistCache = await Gateway.GetGistAsync(GistId).ConfigAwait();
        }
        
        public async Task LoadAllTruncatedFilesAsync()
        {
            var gist = await GetGistAsync().ConfigAwait();

            var files = gist.Files.Where(x => 
                (string.IsNullOrEmpty(x.Value.Content) || x.Value.Content.Length < x.Value.Size) && x.Value.Truncated);

            var tasks = files.Select(async x => {
                x.Value.Content = await x.Value.Raw_Url.GetStringFromUrlAsync().ConfigAwait();
            });

            await Task.WhenAll(tasks).ConfigAwait();
        }

        public void ClearGist() => gistCache = null;

        public override IVirtualFile GetFile(string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
                return null;

            var filePath = SanitizePath(virtualPath);
            var gist = GetGist();

            if (!GetGistContents(filePath, gist, out var text, out var stream))
                return null;

            var dirPath = GetDirPath(filePath);
            return new GistVirtualFile(this, new GistVirtualDirectory(this, dirPath, GetParentDirectory(dirPath)))
                .Init(filePath, gist.Updated_At ?? gist.Created_At, text, stream);
        }

        private GistVirtualDirectory GetParentDirectory(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
                return null;

            var parentDir = GetDirPath(dirPath.TrimEnd(DirSep));
            return parentDir != null
                ? new GistVirtualDirectory(this, parentDir, GetParentDirectory(parentDir))
                : (GistVirtualDirectory) RootDirectory;
        }

        public override IVirtualDirectory GetDirectory(string virtualPath)
        {
            if (virtualPath == null)
                return null;

            var dirPath = SanitizePath(virtualPath);
            if (string.IsNullOrEmpty(dirPath))
                return RootDirectory;

            var seekPath = dirPath[dirPath.Length - 1] != DirSep
                ? dirPath + DirSep
                : dirPath;

            var gist = GetGist();
            foreach (var entry in gist.Files)
            {
                if (entry.Key.StartsWith(seekPath))
                    return new GistVirtualDirectory(this, dirPath, GetParentDirectory(dirPath));
            }

            return null;
        }

        public override bool DirectoryExists(string virtualPath)
        {
            return GetDirectory(virtualPath) != null;
        }

        public override bool FileExists(string virtualPath)
        {
            return GetFile(virtualPath) != null;
        }

        public override void WriteFiles(Dictionary<string, string> textFiles)
        {
            var gistFiles = new Dictionary<string, string>();
            foreach (var entry in textFiles)
            {
                var filePath = SanitizePath(entry.Key);
                gistFiles[filePath] = entry.Value;
            }

            Gateway.WriteGistFiles(GistId, gistFiles);
            ClearGist();
        }

        public override void WriteFiles(Dictionary<string, object> files)
        {
            Gateway.WriteGistFiles(GistId, files);
            ClearGist();
        }

        public void WriteFile(string virtualPath, string contents)
        {
            var filePath = SanitizePath(virtualPath);
            Gateway.WriteGistFile(GistId, filePath, contents);
            ClearGist();
        }

        public void WriteFile(string virtualPath, Stream stream)
        {
            var base64 = ToBase64(stream);
            var filePath = SanitizePath(virtualPath) + Base64Modifier;
            Gateway.WriteGistFile(GistId, filePath, base64);
            ClearGist();
        }

        public static string ToBase64(Stream stream)
        {
            var base64 = stream is MemoryStream ms
                ? Convert.ToBase64String(ms.GetBuffer(), 0, (int) ms.Length)
                : Convert.ToBase64String(stream.ReadFully());
            return base64;
        }

        public static string ToBase64(byte[] bytes) => Convert.ToBase64String(bytes);

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            this.CopyFrom(files, toPath);
            ClearGist();
        }

        public void AppendFile(string filePath, string textContents)
        {
            throw new NotImplementedException("Gists doesn't support appending to files");
        }

        public void AppendFile(string filePath, Stream stream)
        {
            throw new NotImplementedException("Gists doesn't support appending to files");
        }

        public string ResolveGistFileName(string filePath)
        {
            var gist = GetGist();
            var baseFilePath = filePath + Base64Modifier;
            foreach (var entry in gist.Files)
            {
                if (entry.Key == filePath || entry.Key == baseFilePath)
                    return entry.Key;
            }

            return null;
        }

        public void DeleteFile(string filePath)
        {
            filePath = SanitizePath(filePath);
            filePath = ResolveGistFileName(filePath) ?? filePath;
            Gateway.DeleteGistFiles(GistId, filePath);
            ClearGist();
        }

        public void DeleteFiles(IEnumerable<string> virtualFilePaths)
        {
            var filePaths = virtualFilePaths.Map(x => {
                var filePath = SanitizePath(x);
                return ResolveGistFileName(filePath) ?? filePath;
            });
            Gateway.DeleteGistFiles(GistId, filePaths.ToArray());
            ClearGist();
        }

        public void DeleteFolder(string dirPath)
        {
            dirPath = SanitizePath(dirPath);
            var nestedFiles = EnumerateFiles(dirPath).Map(x => x.FilePath);
            DeleteFiles(nestedFiles);
        }

        public IEnumerable<GistVirtualFile> EnumerateFiles(string prefix = null)
        {
            var gist = GetGist();

            foreach (var entry in gist.Files)
            {
                if (!GetGistContents(entry.Key, gist, out var text, out var stream))
                    continue;

                var filePath = SanitizePath(entry.Key);
                var dirPath = GetDirPath(filePath);

                if (prefix != null && (dirPath == null || !dirPath.StartsWith(prefix)))
                    continue;

                yield return new GistVirtualFile(this,
                        new GistVirtualDirectory(this, dirPath, GetParentDirectory(dirPath)))
                    .Init(filePath, gist.Updated_At ?? gist.Created_At, text, stream);
            }
        }

        public override IEnumerable<IVirtualFile> GetAllFiles()
        {
            return EnumerateFiles();
        }

        public IEnumerable<GistVirtualDirectory> GetImmediateDirectories(string fromDirPath)
        {
            var dirPaths = EnumerateFiles(fromDirPath)
                .Map(x => x.DirPath)
                .Distinct()
                .Map(x => GetImmediateSubDirPath(fromDirPath, x))
                .Where(x => x != null)
                .Distinct();

            var parentDir = GetParentDirectory(fromDirPath);
            return dirPaths.Map(x => new GistVirtualDirectory(this, x, parentDir));
        }

        public IEnumerable<GistVirtualFile> GetImmediateFiles(string fromDirPath)
        {
            return EnumerateFiles(fromDirPath)
                .Where(x => x.DirPath == fromDirPath);
        }

        public string GetDirPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var lastDirPos = filePath.LastIndexOf(DirSep);
            return lastDirPos >= 0
                ? filePath.Substring(0, lastDirPos)
                : null;
        }

        public string GetImmediateSubDirPath(string fromDirPath, string subDirPath)
        {
            if (string.IsNullOrEmpty(subDirPath))
                return null;

            if (fromDirPath == null)
            {
                return subDirPath.CountOccurrencesOf(DirSep) == 0
                    ? subDirPath
                    : subDirPath.LeftPart(DirSep);
            }

            if (!subDirPath.StartsWith(fromDirPath))
                return null;

            return fromDirPath.CountOccurrencesOf(DirSep) == subDirPath.CountOccurrencesOf(DirSep) - 1
                ? subDirPath
                : null;
        }

        public override string SanitizePath(string filePath)
        {
            var sanitizedPath = string.IsNullOrEmpty(filePath)
                ? null
                : (IsDirSep(filePath[0]) ? filePath.Substring(1) : filePath);

            return sanitizedPath?.Replace('/', DirSep);
        }

        public static string GetFileName(string filePath) => filePath.LastRightPart(DirSep);
    }

    public class GistVirtualFile : AbstractVirtualFileBase
    {
        private GistVirtualFiles PathProvider { get; set; }

        public IGistGateway Client => PathProvider.Gateway;

        public string GistId => PathProvider.GistId;

        public override string Extension => Name.LastRightPart('.').LeftPart('|');

        public GistVirtualFile(GistVirtualFiles pathProvider, IVirtualDirectory directory)
            : base(pathProvider, directory)
        {
            this.PathProvider = pathProvider;
        }

        public string DirPath => ((GistVirtualDirectory) base.Directory).DirPath;

        public string FilePath { get; set; }

        public string ContentType { get; set; }

        public override string Name => GistVirtualFiles.GetFileName(FilePath);

        public override string VirtualPath => FilePath.Replace('\\', '/');

        public DateTime FileLastModified { get; set; }

        public override DateTime LastModified => FileLastModified;

        public override long Length => ContentLength;

        public long ContentLength { get; set; }

        public string Text { get; set; } // Empty for Binary Files
        public Stream Stream { get; set; }

        public GistVirtualFile Init(string filePath, DateTime lastModified, string text, MemoryStream stream)
        {
            FilePath = filePath;
            ContentType = MimeTypes.GetMimeType(filePath);
            FileLastModified = lastModified;
            ContentLength = stream.Length;
            Text = text;
            Stream = stream;
            return this;
        }

        public override Stream OpenRead()
        {
            Stream.Position = 0;
            return Stream.CopyToNewMemoryStream();
        }

        public override object GetContents()
        {
            return Text != null 
                ? (object) Text.AsMemory() 
                : (Stream is MemoryStream ms
                    ? ms.GetBufferAsMemory()
                    : Stream?.CopyToNewMemoryStream().GetBufferAsMemory());
        }

        public override byte[] ReadAllBytes() => ((MemoryStream)Stream).GetBufferAsBytes();

        public override void Refresh()
        {
            var elapsed = DateTime.UtcNow - PathProvider.LastRefresh;
            var shouldRefresh = elapsed > PathProvider.RefreshAfter;
            var gist = PathProvider.GetGist(refresh: shouldRefresh);
            if (gist != null)
            {
                if (!GistVirtualFiles.GetGistContents(FilePath, gist, out var text, out var stream))
                    throw new FileNotFoundException("Gist File no longer exists", FilePath);

                Init(FilePath, gist.Updated_At ?? gist.Created_At, text, stream);
                return;
            }

            throw new FileNotFoundException("Gist no longer exists", GistId);
        }
    }

    public class GistVirtualDirectory : AbstractVirtualDirectoryBase
    {
        internal GistVirtualFiles PathProvider { get; private set; }

        public GistVirtualDirectory(GistVirtualFiles pathProvider, string dirPath, GistVirtualDirectory parentDir)
            : base(pathProvider, parentDir)
        {
            this.PathProvider = pathProvider;
            this.DirPath = dirPath;
        }

        public DateTime DirLastModified { get; set; }

        public override DateTime LastModified => DirLastModified;

        public override IEnumerable<IVirtualFile> Files => PathProvider.GetImmediateFiles(DirPath);

        public override IEnumerable<IVirtualDirectory> Directories => PathProvider.GetImmediateDirectories(DirPath);

        public IGistGateway Gateway => PathProvider.Gateway;

        public string GistId => PathProvider.GistId;

        public string DirPath { get; set; }

        public override string VirtualPath => DirPath?.Replace('\\', '/');

        public override string Name => DirPath?.LastRightPart(GistVirtualFiles.DirSep);

        public override IVirtualFile GetFile(string virtualPath)
        {
            return VirtualPathProvider.GetFile(DirPath.CombineWith(virtualPath));
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
        {
            return GetFile(fileName);
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        {
            var matchingFilesInBackingDir = EnumerateFiles(globPattern);
            return matchingFilesInBackingDir;
        }

        public IEnumerable<GistVirtualFile> EnumerateFiles(string pattern)
        {
            foreach (var file in PathProvider.GetImmediateFiles(DirPath).Where(f => f.Name.Glob(pattern)))
            {
                yield return file;
            }
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return new GistVirtualDirectory(PathProvider, PathProvider.SanitizePath(DirPath.CombineWith(directoryName)),
                this);
        }

        public void AddFile(string virtualPath, string contents)
        {
            VirtualPathProvider.WriteFile(DirPath.CombineWith(virtualPath), contents);
        }

        public void AddFile(string virtualPath, Stream stream)
        {
            VirtualPathProvider.WriteFile(DirPath.CombineWith(virtualPath), stream);
        }

        private static string StripDirSeparatorPrefix(string filePath)
        {
            return string.IsNullOrEmpty(filePath)
                ? filePath
                : (filePath[0] == GistVirtualFiles.DirSep ? filePath.Substring(1) : filePath);
        }

        public override IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = int.MaxValue)
        {
            if (IsRoot)
            {
                return PathProvider.EnumerateFiles().Where(x =>
                    (x.DirPath == null || x.DirPath.CountOccurrencesOf(GistVirtualFiles.DirSep) < maxDepth - 1)
                    && x.Name.Glob(globPattern));
            }

            return PathProvider.EnumerateFiles(DirPath).Where(x =>
                x.DirPath != null
                && x.DirPath.CountOccurrencesOf(GistVirtualFiles.DirSep) < maxDepth - 1
                && x.DirPath.StartsWith(DirPath)
                && x.Name.Glob(globPattern));
        }
    }
}