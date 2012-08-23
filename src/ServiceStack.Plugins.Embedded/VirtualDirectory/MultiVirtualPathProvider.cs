using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Plugins.Embedded.VirtualPath
{
    public class MultiVirtualPathProvider : AbstractVirtualPathProviderBase
    {
        protected IList<IVirtualPathProvider> ChildProviders;

        public override IVirtualDirectory RootDirectory
        {
            get { throw new NotImplementedException("Makes no sense here"); }
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
            return Path.Combine(basePath, relativePath);
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return ChildProviders.Select(p => p.GetFile(virtualPath))
                .FirstOrDefault();
        }

        public override IVirtualDirectory GetDirectory(string virtualPath)
        {
            return ChildProviders.Select(p => p.GetDirectory(virtualPath))
                .FirstOrDefault();
        }

        public override IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue)
        {
            return ChildProviders.SelectMany(p => p.GetAllMatchingFiles(globPattern, maxDepth))
                .Distinct();
        }

        public override bool IsSharedFile(IVirtualFile virtualFile)
        {
            return virtualFile.VirtualPathProvider.IsSharedFile(virtualFile);
        }

        public override bool IsViewFile(IVirtualFile virtualFile)
        {
            return virtualFile.VirtualPathProvider.IsViewFile(virtualFile);
        }
    }
}
