using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public class MultiVirtualPathProvider : AbstractVirtualPathProviderBase
    {
        protected IList<IVirtualPathProvider> ChildProviders;

        public override IVirtualDirectory RootDirectory
        {
            get { return ChildProviders.FirstOrDefault().RootDirectory; }
        }

        public override String VirtualPathSeparator { get { return "/"; } }
        public override string RealPathSeparator { get { return Convert.ToString(Path.DirectorySeparatorChar); } }

        public MultiVirtualPathProvider(IAppHost appHost, params IVirtualPathProvider[] childProviders) 
            : base(appHost)
        {
            if (childProviders == null || childProviders.Length == 0)
                throw new ArgumentException("childProviders");

            this.ChildProviders = new List<IVirtualPathProvider>(childProviders);
            Initialize();
        }

        protected override sealed void Initialize() {}

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
            return ChildProviders.SelectMany(x => x.RootDirectory.Files);
        }

        public override IEnumerable<IVirtualDirectory> GetRootDirectories()
        {
            return ChildProviders.SelectMany(x => x.RootDirectory.Directories);
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
    }
}
