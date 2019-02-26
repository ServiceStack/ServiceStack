using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public class MultiVirtualFiles 
        : AbstractVirtualPathProviderBase, IVirtualFiles
    {
        public List<IVirtualPathProvider> ChildProviders { get; set; }

        public override IVirtualDirectory RootDirectory => ChildProviders.FirstOrDefault().RootDirectory;

        public override string VirtualPathSeparator => "/";
        public override string RealPathSeparator => Convert.ToString(Path.DirectorySeparatorChar);

        public MultiVirtualFiles(params IVirtualPathProvider[] childProviders) 
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

        public override bool DirectoryExists(string virtualPath)
        {
            var hasDirectory = ChildProviders.Any(childProvider => childProvider.DirectoryExists(virtualPath));
            return hasDirectory;
        }

        public override bool FileExists(string virtualPath)
        {
            var hasFile = ChildProviders.Any(childProvider => childProvider.FileExists(virtualPath));
            return hasFile;
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return ChildProviders.Select(childProvider => childProvider.GetFile(virtualPath))
                .FirstOrDefault(file => file != null);
        }

        public override IVirtualDirectory GetDirectory(string virtualPath) => 
            MultiVirtualDirectory.ToVirtualDirectory(ChildProviders.Select(p => p.GetDirectory(virtualPath)).Where(dir => dir != null));

        public override IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = int.MaxValue)
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

    public class MultiVirtualDirectory : IVirtualDirectory
    {
        public static IVirtualDirectory ToVirtualDirectory(IEnumerable<IVirtualDirectory> dirs)
        {
            var arr = dirs.ToArray();
            return arr.Length == 0
                ? null
                : arr.Length == 1
                    ? arr[0]
                    : new MultiVirtualDirectory(arr);
        }

        private readonly IVirtualDirectory[] dirs;

        public MultiVirtualDirectory(IVirtualDirectory[] dirs)
        {
            if (dirs.Length == 0)
                throw new ArgumentNullException(nameof(dirs));

            this.dirs = dirs;
        }

        public IVirtualDirectory Directory => this;
        public string Name => this.First().Name;
        public string VirtualPath => this.First().VirtualPath;
        public string RealPath => this.First().RealPath;
        public bool IsDirectory => true;
        public DateTime LastModified => this.First().LastModified;

        public IEnumerator<IVirtualNode> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsRoot => this.dirs.First().IsRoot;

        public IVirtualDirectory ParentDirectory =>
            ToVirtualDirectory(dirs.SelectMany(x => x.ParentDirectory).Where(x => x != null).Cast<IVirtualDirectory>());

        public IEnumerable<IVirtualFile> Files => dirs.SelectMany(x => x.Files);

        public IEnumerable<IVirtualDirectory> Directories => dirs.SelectMany(x => x.Directories);

        public IVirtualFile GetFile(string virtualPath)
        {
            foreach (var dir in dirs)
            {
                var file = dir.GetFile(virtualPath);
                if (file != null)
                    return file;
            }
            return null;
        }

        public IVirtualFile GetFile(Stack<string> virtualPath)
        {
            foreach (var dir in dirs)
            {
                var file = dir.GetFile(virtualPath);
                if (file != null)
                    return file;
            }
            return null;
        }

        public IVirtualDirectory GetDirectory(string virtualPath)
        {
            foreach (var dir in dirs)
            {
                var sub = dir.GetDirectory(virtualPath);
                if (sub != null)
                    return sub;
            }
            return null;
        }

        public IVirtualDirectory GetDirectory(Stack<string> virtualPath)
        {
            foreach (var dir in dirs)
            {
                var sub = dir.GetDirectory(virtualPath);
                if (sub != null)
                    return sub;
            }
            return null;
        }

        public IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue)
        {
            foreach (var dir in dirs)
            {
                var files = dir.GetAllMatchingFiles(globPattern, maxDepth);
                foreach (var file in files)
                {
                    yield return file;
                }
            }
        }
    }
}