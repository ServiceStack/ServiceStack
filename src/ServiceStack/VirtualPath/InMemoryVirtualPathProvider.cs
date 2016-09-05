using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    [Obsolete("Renamed to IVirtualFiles")]
    public interface IWriteableVirtualPathProvider : IVirtualFiles { }

    /// <summary>
    /// In Memory Virtual Path Provider.
    /// </summary>
    public class InMemoryVirtualPathProvider : AbstractVirtualPathProviderBase, IVirtualFiles, IWriteableVirtualPathProvider
    {
        public InMemoryVirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            this.files = new List<InMemoryVirtualFile>();
            this.rootDirectory = new InMemoryVirtualDirectory(this, null);
        }

        public List<InMemoryVirtualFile> files;

        public InMemoryVirtualDirectory rootDirectory;

        public const char DirSep = '/';

        public override IVirtualDirectory RootDirectory => rootDirectory;

        public override string VirtualPathSeparator => "/";

        public override string RealPathSeparator => "/";

        protected override void Initialize() {}

        public override IVirtualFile GetFile(string virtualPath)
        {
            var filePath = SanitizePath(virtualPath);
            return files.FirstOrDefault(x => x.FilePath == filePath);
        }

        public IVirtualDirectory GetDirectory(string dirPath)
        {
            var dir = new InMemoryVirtualDirectory(this, dirPath);
            return dir.Files.Any()
                ? dir
                : null;
        }

        public override bool DirectoryExists(string virtualPath)
        {
            return GetDirectory(virtualPath) != null;
        }

        private IVirtualDirectory CreateDirectory(string dirPath)
        {
            return new InMemoryVirtualDirectory(this, dirPath);
        }

        public void WriteFile(string filePath, string textContents)
        {
            filePath = SanitizePath(filePath);
            DeleteFile(filePath);
            this.files.Add(new InMemoryVirtualFile(this, CreateDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                TextContents = textContents,
                FileLastModified = DateTime.UtcNow,
            });
        }

        public void WriteFile(string filePath, Stream stream)
        {
            filePath = SanitizePath(filePath);
            DeleteFile(filePath);
            this.files.Add(new InMemoryVirtualFile(this, CreateDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                ByteContents = stream.ReadFully(),
                FileLastModified = DateTime.UtcNow,
            });
        }

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            this.CopyFrom(files, toPath);
        }

        public void AppendFile(string filePath, string textContents)
        {
            filePath = SanitizePath(filePath);

            var existingFile = GetFile(filePath);
            var text = existingFile != null
                ? existingFile.ReadAllText() + textContents
                : textContents;

            DeleteFile(filePath);

            this.files.Add(new InMemoryVirtualFile(this, CreateDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                TextContents = text,
                FileLastModified = DateTime.UtcNow,
            });
        }

        public void AppendFile(string filePath, Stream stream)
        {
            filePath = SanitizePath(filePath);

            var existingFile = GetFile(filePath);
            var bytes = existingFile != null
                ? existingFile.ReadAllBytes().Combine(stream.ReadFully())
                : stream.ReadFully();

            DeleteFile(filePath);

            this.files.Add(new InMemoryVirtualFile(this, CreateDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                ByteContents = bytes,
                FileLastModified = DateTime.UtcNow,
            });
        }

        public void DeleteFile(string filePath)
        {
            filePath = SanitizePath(filePath);
            this.files.RemoveAll(x => x.FilePath == filePath);
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            filePaths.Each(DeleteFile);
        }

        public void DeleteFolder(string dirPath)
        {
            var subFiles = files.Where(x => x.DirPath.StartsWith(dirPath));
            DeleteFiles(subFiles.Map(x => x.VirtualPath));            
        }

        public IEnumerable<InMemoryVirtualDirectory> GetImmediateDirectories(string fromDirPath)
        {
            var dirPaths = files
                .Map(x => x.DirPath)
                .Distinct()
                .Map(x => GetImmediateSubDirPath(fromDirPath, x))
                .Where(x => x != null)
                .Distinct();

            return dirPaths.Map(x => new InMemoryVirtualDirectory(this, x));
        }

        public IEnumerable<InMemoryVirtualFile> GetImmediateFiles(string fromDirPath)
        {
            return files.Where(x => x.DirPath == fromDirPath);
        }

        public override IEnumerable<IVirtualFile> GetAllFiles()
        {
            return files;
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
                    : null;
            }

            if (!subDirPath.StartsWith(fromDirPath))
                return null;

            return fromDirPath.CountOccurrencesOf(DirSep) == subDirPath.CountOccurrencesOf(DirSep) - 1 
                ? subDirPath
                : null;
        }

        public string SanitizePath(string filePath)
        {
            var sanitizedPath = string.IsNullOrEmpty(filePath)
                ? null
                : (filePath[0] == DirSep ? filePath.Substring(1) : filePath);

            return sanitizedPath?.Replace('\\', DirSep);
        }
    }

    public class InMemoryVirtualDirectory : AbstractVirtualDirectoryBase
    {
        private readonly InMemoryVirtualPathProvider pathProvider;

        public InMemoryVirtualDirectory(InMemoryVirtualPathProvider pathProvider, string dirPath) 
            : base(pathProvider)
        {
            this.pathProvider = pathProvider;
            this.DirPath = dirPath;
        }
        
        public DateTime DirLastModified { get; set; }
        public override DateTime LastModified => DirLastModified;

        public override IEnumerable<IVirtualFile> Files => pathProvider.GetImmediateFiles(DirPath);

        public override IEnumerable<IVirtualDirectory> Directories => pathProvider.GetImmediateDirectories(DirPath);

        public string DirPath { get; set; }

        public override string VirtualPath => DirPath;

        public override string Name => DirPath?.LastRightPart(InMemoryVirtualPathProvider.DirSep);

        public override IVirtualFile GetFile(string virtualPath)
        {
            return pathProvider.GetFile(DirPath.CombineWith(virtualPath));
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

        public IEnumerable<InMemoryVirtualFile> EnumerateFiles(string pattern)
        {
            foreach (var file in pathProvider.GetImmediateFiles(DirPath).Where(f => f.Name.Glob(pattern)))
            {
                yield return file;
            }
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            var subDir = DirPath.CombineWith(directoryName);
            return new InMemoryVirtualDirectory(pathProvider, subDir);
        }

        public void AddFile(string filePath, string contents)
        {
            pathProvider.WriteFile(DirPath.CombineWith(filePath), contents);
        }

        public void AddFile(string filePath, Stream stream)
        {
            pathProvider.WriteFile(DirPath.CombineWith(filePath), stream);
        }
    }

    public class InMemoryVirtualFile : AbstractVirtualFileBase
    {
        public InMemoryVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory) 
            : base(owningProvider, directory)
        {
            this.FileLastModified = DateTime.MinValue;            
        }

        public string DirPath => base.Directory.VirtualPath;

        public string FilePath { get; set; }

        public override string Name => FilePath.LastRightPart(InMemoryVirtualPathProvider.DirSep);

        public override string VirtualPath => FilePath;

        public DateTime FileLastModified { get; set; }
        public override DateTime LastModified => FileLastModified;

        public override long Length => TextContents?.Length ?? (ByteContents?.Length ?? 0);

        public string TextContents { get; set; }

        public byte[] ByteContents { get; set; }

        public override Stream OpenRead()
        {
            return MemoryStreamFactory.GetStream(ByteContents ?? (TextContents ?? "").ToUtf8Bytes());
        }

        public override void Refresh()
        {
            var file = base.VirtualPathProvider.GetFile(VirtualPath) as InMemoryVirtualFile;
            if (file != null)
            {
                this.FilePath = file.FilePath;
                this.FileLastModified = file.FileLastModified;
                this.TextContents = file.TextContents;
                this.ByteContents = file.ByteContents;
            }
        }
    }
}