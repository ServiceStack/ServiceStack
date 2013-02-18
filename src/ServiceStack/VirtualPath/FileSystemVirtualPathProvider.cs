using System;
using System.IO;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.VirtualPath
{
    public class FileSystemVirtualPathProvider : AbstractVirtualPathProviderBase
    {
        protected DirectoryInfo RootDirInfo;
        protected FileSystemVirtualDirectory RootDir;

        public override IVirtualDirectory RootDirectory { get { return RootDir; } }
        public override String VirtualPathSeparator { get { return "/"; } }
        public override string RealPathSeparator { get { return Convert.ToString(Path.DirectorySeparatorChar); } }

        public FileSystemVirtualPathProvider(IAppHost appHost, String rootDirectoryPath)
            : this(appHost, new DirectoryInfo(rootDirectoryPath))
        { }

        public FileSystemVirtualPathProvider(IAppHost appHost, DirectoryInfo rootDirInfo)
            : base(appHost)
        {
            if (rootDirInfo == null)
                throw new ArgumentNullException("rootDirInfo");

            this.RootDirInfo = rootDirInfo;
            Initialize();
        }

        public FileSystemVirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            Initialize();
        }

        protected override sealed void Initialize()
        {
            if (RootDirInfo == null)
                RootDirInfo = new DirectoryInfo(AppHost.Config.WebHostPhysicalPath);

            if (RootDirInfo == null || ! RootDirInfo.Exists)
                throw new ApplicationException(
                    "RootDir '{0}' for virtual path does not exist".Fmt(RootDirInfo.FullName));

            RootDir = new FileSystemVirtualDirectory(this, null, RootDirInfo);
        }
       
    }
}
