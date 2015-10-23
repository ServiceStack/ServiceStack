using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    /// <summary>
    /// In Memory repository for files. Useful for testing.
    /// </summary>
    public class InMemoryVirtualPathProvider : AbstractVirtualPathProviderBase, IWriteableVirtualPathProvider
    {
        public InMemoryVirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            this.files = new List<InMemoryVirtualFile>();
            this.rootDirectory = new InMemoryVirtualDirectory(this, null);
        }

        public List<InMemoryVirtualFile> files;

        public InMemoryVirtualDirectory rootDirectory;

        public static readonly char DirSep = '/';

        public override IVirtualDirectory RootDirectory
        {
            get { return rootDirectory; }
        }

        public override string VirtualPathSeparator
        {
            get { return "/"; }
        }

        public override string RealPathSeparator
        {
            get { return "/"; }
        }

        protected override void Initialize()
        {
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return files.FirstOrDefault(x => x.FilePath == virtualPath);
        }

        public IVirtualDirectory GetDirectory(string dirPath)
        {
            return new InMemoryVirtualDirectory(this, dirPath);
        }

        public void AddFile(string filePath, string textContents)
        {
            this.files.Add(new InMemoryVirtualFile(this, GetDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                FileName = filePath.Split(DirSep).Last(),
                TextContents = textContents,
            });
        }

        public void AddFile(string filePath, Stream stream)
        {
            this.files.Add(new InMemoryVirtualFile(this, GetDirectory(GetDirPath(filePath)))
            {
                FilePath = filePath,
                FileName = filePath.Split(DirSep).Last(),
                ByteContents = stream.ReadFully(),
            });
        }

        public IEnumerable<InMemoryVirtualDirectory> GetImmediateDirectories(string fromDirPath)
        {
            var dirPaths = files
                .Map(x => x.DirPath)
                .Distinct()
                .Map(x => GetSubDirPath(fromDirPath, x))
                .Where(x => x != null)
                .Distinct();

            return dirPaths.Map(x => new InMemoryVirtualDirectory(this, x));
        }

        public IEnumerable<InMemoryVirtualFile> GetImmediateFiles(string fromDirPath)
        {
            return files.Where(x => x.DirPath == fromDirPath);
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

        public string GetSubDirPath(string fromDirPath, string subDirPath)
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
        public override DateTime LastModified
        {
            get { return DirLastModified; }
        }

        public override IEnumerable<IVirtualFile> Files
        {
            get { return pathProvider.GetImmediateFiles(DirPath); }
        }

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get { return pathProvider.GetImmediateDirectories(DirPath); }
        }

        public string DirPath { get; set; }
        public override string Name
        {
            get { return DirPath; }
        }

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
            var matchingFilesInBackingDir = EnumerateFiles(globPattern).Cast<IVirtualFile>();
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
            pathProvider.AddFile(DirPath.CombineWith(filePath), contents);
        }

        public void AddFile(string filePath, Stream stream)
        {
            pathProvider.AddFile(DirPath.CombineWith(filePath), stream);
        }
    }

    public class InMemoryVirtualFile : AbstractVirtualFileBase
    {
        public InMemoryVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory) 
            : base(owningProvider, directory)
        {
            this.FileLastModified = DateTime.MinValue;            
        }

        public string DirPath
        {
            get { return base.Directory.Name; }
        }

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public override string Name
        {
            get { return FilePath; }
        }

        public DateTime FileLastModified { get; set; }
        public override DateTime LastModified
        {
            get { return FileLastModified; }
        }

        public override long Length
        {
            get
            {
                return TextContents != null ? 
                    TextContents.Length 
                      : ByteContents != null ? 
                    ByteContents.Length : 
                    0;
            }
        }

        public string TextContents { get; set; }

        public byte[] ByteContents { get; set; }

        public override Stream OpenRead()
        {
            return MemoryStreamFactory.GetStream(ByteContents ?? (TextContents ?? "").ToUtf8Bytes());
        }
    }
}