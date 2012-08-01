using System;
using System.IO;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Text;

namespace ServiceStack.Razor.VirtualPath
{
    public class FileSystemVirtualPathProvider : AbstractVirtualPathProviderBase
    {
        #region Fields

        protected DirectoryInfo rootDirInfo;
        protected FileSystemVirtualDirectory rootDir;

        #endregion

        public FileSystemVirtualPathProvider(IAppHost appHost, String rootDirectoryPath)
            : this(appHost, new DirectoryInfo(rootDirectoryPath))
        { }

        public FileSystemVirtualPathProvider(IAppHost appHost, DirectoryInfo rootDirInfo)
            : base(appHost)
        {
            if (rootDirInfo == null)
                throw new ArgumentNullException("rootDirInfo");

            this.rootDirInfo = rootDirInfo;
            Initialize();
        }

        public FileSystemVirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            Initialize();
        }

        protected override sealed void Initialize()
        {
            if (rootDirInfo == null)
                rootDirInfo = new DirectoryInfo(AppHost.Config.MarkdownSearchPath);

            if (rootDirInfo == null || ! rootDirInfo.Exists)
                throw new ApplicationException(String.Format("RootDir '{0}' for virtual path does not exist",
                                                             rootDirInfo.FullName));

            rootDir = new FileSystemVirtualDirectory(this, null, rootDirInfo);
        }

        #region Properties

        public override IVirtualDirectory RootDirectory { get { return rootDir; } }

        public override String VirtualPathSeparator { get { return "/"; } }
        public override string RealPathSeparator { get { return Convert.ToString(Path.DirectorySeparatorChar); } }

        #endregion
       
    }
}
