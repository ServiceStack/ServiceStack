using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public class MultiVirtualFiles : MultiVirtualPathProvider
    {
        public MultiVirtualFiles(params IVirtualPathProvider[] childProviders) : base(childProviders) {}
    }
    
    [Obsolete("Renamed to MultiVirtualFiles")]
    public class MultiVirtualPathProvider : AbstractVirtualPathProviderBase, IVirtualFiles
    {
        public List<IVirtualPathProvider> ChildProviders { get; set; }

        public override IVirtualDirectory RootDirectory => ChildProviders.FirstOrDefault().RootDirectory;

        public override string VirtualPathSeparator => "/";
        public override string RealPathSeparator => Convert.ToString(Path.DirectorySeparatorChar);

        public MultiVirtualPathProvider(params IVirtualPathProvider[] childProviders) 
        {
            if (childProviders == null || childProviders.Length == 0)
                throw new ArgumentNullException(nameof(childProviders));

            this.ChildProviders = new List<IVirtualPathProvider>(childProviders);
            Initialize();
        }

        protected sealed override void Initialize() {}

        public override string CombineVirtualPath(string basePath, string relativePath)
        {
            return basePath.CombineWith(relativePath);
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return ChildProviders.Select(childProvider => childProvider.GetFile(virtualPath))
                .FirstOrDefault(file => file != null);
        }

        public override IVirtualDirectory GetDirectory(string virtualPath)
        {
            return ChildProviders.Select(p => p.GetDirectory(virtualPath))
                .FirstOrDefault(dir => dir != null);
        }

        public override IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue)
        {
            return ChildProviders.SelectMany(p => p.GetAllMatchingFiles(globPattern, maxDepth))
                .Distinct();
        }

        public override IEnumerable<IVirtualFile> GetRootFiles()
        {
            return ChildProviders.SelectMany(x => x.GetRootFiles());
        }

        public override IEnumerable<IVirtualDirectory> GetRootDirectories()
        {
            return ChildProviders.SelectMany(x => x.GetRootDirectories());
        }

        public override bool IsSharedFile(IVirtualFile virtualFile)
        {
            return virtualFile.VirtualPathProvider.IsSharedFile(virtualFile);
        }

        public override bool IsViewFile(IVirtualFile virtualFile)
        {
            return virtualFile.VirtualPathProvider.IsViewFile(virtualFile);
        }

        public override string ToString()
        {
            var sb = new List<string>();
            ChildProviders.Each(x => sb.Add(x.ToString()));

            return string.Join(", ", sb.ToArray());
        }

        public IEnumerable<IVirtualPathProvider> ChildVirtualFiles
        {
            get { return ChildProviders.Where(x => x is IVirtualFiles); }
        }

        public void WriteFile(string filePath, string textContents)
        {
            ChildVirtualFiles.Each(x => x.WriteFile(filePath, textContents));
        }

        public void WriteFile(string filePath, Stream stream)
        {
            ChildVirtualFiles.Each(x => x.WriteFile(filePath, stream));
        }

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            ChildVirtualFiles.Each(x => x.WriteFiles(files, toPath));
        }

        public void AppendFile(string filePath, string textContents)
        {
            ChildVirtualFiles.Each(x => x.AppendFile(filePath, textContents));
        }

        public void AppendFile(string filePath, Stream stream)
        {
            ChildVirtualFiles.Each(x => x.AppendFile(filePath, stream));
        }

        public void DeleteFile(string filePath)
        {
            ChildVirtualFiles.Each(x => x.DeleteFile(filePath));
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            ChildVirtualFiles.Each(x => x.DeleteFiles(filePaths));
        }

        public void DeleteFolder(string dirPath)
        {
            ChildVirtualFiles.Each(x => x.DeleteFolder(dirPath));
        }
    }
}
