using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.VirtualPath
{
    public interface IWriteableVirtualPathProvider
    {
        void AddFile(string filePath, string contents);
    }

    /// <summary>
    /// In Memory repository for files. Useful for testing.
    /// </summary>
    public class InMemoryVirtualPathProvider : AbstractVirtualPathProviderBase, IWriteableVirtualPathProvider
    {
        public InMemoryVirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            this.rootDirectory = new InMemoryVirtualDirectory(this);
        }

        public InMemoryVirtualDirectory rootDirectory;

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

        public void AddFile(string filePath, string contents)
        {
            rootDirectory.AddFile(filePath, contents);
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return rootDirectory.GetFile(virtualPath)
                ?? base.GetFile(virtualPath);
        }
    }

    public class InMemoryVirtualDirectory : AbstractVirtualDirectoryBase
    {
        public InMemoryVirtualDirectory(IVirtualPathProvider owningProvider) 
            : base(owningProvider)
        {
            this.files = new List<InMemoryVirtualFile>();
            this.dirs = new List<InMemoryVirtualDirectory>();
            this.DirLastModified = DateTime.MinValue;
        }
        
        public InMemoryVirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory) 
            : base(owningProvider, parentDirectory) {}

        public DateTime DirLastModified { get; set; }
        public override DateTime LastModified
        {
            get { return DirLastModified; }
        }

        public List<InMemoryVirtualFile> files;

        public override IEnumerable<IVirtualFile> Files
        {
            get { return files; }
        }

        public List<InMemoryVirtualDirectory> dirs;

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get { return dirs; }
        }

        public string DirName { get; set; }
        public override string Name
        {
            get { return DirName; }
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            virtualPath = StripBeginningDirectorySeparator(virtualPath);
            return files.FirstOrDefault(x => x.FilePath == virtualPath);
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
            foreach (var file in files.Where(f => f.Name.Glob(pattern)))
            {
                yield return file;
            }
            foreach (var file in dirs.SelectMany(d => d.EnumerateFiles(pattern)))
            {
                yield return file;
            }
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return null;
        }

        static readonly char[] DirSeps = new[] { '\\', '/' };
        public void AddFile(string filePath, string contents)
        {
            filePath = StripBeginningDirectorySeparator(filePath);
            this.files.Add(new InMemoryVirtualFile(VirtualPathProvider, this) {
                FilePath = filePath,
                FileName = filePath.Split(DirSeps).Last(),
                TextContents = contents,
            });
        }

        private static string StripBeginningDirectorySeparator(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                return filePath;

            if (DirSeps.Any(d => filePath[0] == d))
                    return filePath.Substring(1);

            return filePath;
        }
    }
    
    public class InMemoryVirtualFile : AbstractVirtualFileBase
    {
        public InMemoryVirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory) 
            : base(owningProvider, directory)
        {
            this.FileLastModified = DateTime.MinValue;            
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