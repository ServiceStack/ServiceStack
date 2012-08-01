using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor.VirtualPath
{
    public class MultiVirtualPathProvider : AbstractVirtualPathProviderBase
    {
        #region Fields

        protected IList<IVirtualPathProvider> childProviders; 

        #endregion

        public MultiVirtualPathProvider(IAppHost appHost, params IVirtualPathProvider[] childProviders) 
            : base(appHost)
        {
            if (childProviders == null || childProviders.Length == 0)
                throw new ArgumentException("childProviders");

            this.childProviders = new List<IVirtualPathProvider>(childProviders);
            Initialize();
        }

        protected override sealed void Initialize()
        { }

        public override string CombineVirtualPath(string basePath, string relativePath)
        {
            return Path.Combine(basePath, relativePath);
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return childProviders.Select(p => p.GetFile(virtualPath))
                                 .FirstOrDefault();
        }

        public override IVirtualDirectory GetDirectory(string virtualPath)
        {
            return childProviders.Select(p => p.GetDirectory(virtualPath))
                                 .FirstOrDefault();
        }

        public override IEnumerable<IVirtualFile> GetAllMatchingFiles(string globPattern, int maxDepth = Int32.MaxValue)
        {
            return childProviders.SelectMany(p => p.GetAllMatchingFiles(globPattern, maxDepth))
                                 .Distinct();
        }

        #region Properties

        public override IVirtualDirectory RootDirectory
        {
            get { throw new NotImplementedException("Makes no sense here"); }
        }

        public override String VirtualPathSeparator { get { return "/"; } }
        public override string RealPathSeparator { get { return Convert.ToString(Path.DirectorySeparatorChar); } }

        #endregion
    }
}
