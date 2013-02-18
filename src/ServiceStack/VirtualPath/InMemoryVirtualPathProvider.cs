using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.VirtualPath
{
    /// <summary>
    /// In Memory repository for files. Useful for testing.
    /// </summary>
    public class InMemoryVirtualPathProvider : AbstractVirtualPathProviderBase
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
            get { return files.Cast<IVirtualFile>(); }
        }

        public List<InMemoryVirtualDirectory> dirs;

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get { return dirs.Cast<IVirtualDirectory>(); }
        }

        public string DirName { get; set; }
        public override string Name
        {
            get { return DirName; }
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return files.FirstOrDefault(x => x.FilePath == virtualPath);
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
        {
            return files.FirstOrDefault(x => x.FilePath == fileName);
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        {
            throw new NotImplementedException();
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return null;
        }

        static readonly char[] DirSeps = new[] { '\\', '/' };
        public void AddFile(string filePath, string contents)
        {
            this.files.Add(new InMemoryVirtualFile(VirtualPathProvider, this) {
                FilePath = filePath,
                FileName = filePath.Split(DirSeps).Last(),
                TextContents = contents,
            });
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
            get { return FileName; }
        }

        public DateTime FileLastModified { get; set; }
        public override DateTime LastModified
        {
            get { return FileLastModified; }
        }

        public string TextContents { get; set; }

        public byte[] ByteContents { get; set; }

        public override Stream OpenRead()
        {
            return new MemoryStream(ByteContents ?? (TextContents ?? "").ToUtf8Bytes());
        }
    }


}