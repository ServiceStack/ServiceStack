using System;
using System.Reflection;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor.VirtualPath
{
    public class ResourceVirtualPathProvider : AbstractVirtualPathProviderBase
    {
        protected ResourceVirtualDirectory RootDir;
        protected Assembly BackingAssembly;

        public override IVirtualDirectory RootDirectory { get { return RootDir; } }
        public override string VirtualPathSeparator { get { return "/"; } }
        public override string RealPathSeparator { get { return "."; } }

        public ResourceVirtualPathProvider(IAppHost appHost, Type typeInBackingAssembly)
            : base(appHost)
        {
            if (typeInBackingAssembly == null)
                throw new ArgumentNullException("typeInBackingAssembly");

            this.BackingAssembly = typeInBackingAssembly.Assembly;
            Initialize();
        }

        public ResourceVirtualPathProvider(IAppHost appHost, Assembly backingAssembly)
            : base(appHost)
        {
            if (backingAssembly == null)
                throw new ArgumentNullException("backingAssembly");

            this.BackingAssembly = backingAssembly;
            Initialize();
        }

        public ResourceVirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            Initialize();
        }

        protected override sealed void Initialize()
        {
            var asm = BackingAssembly ?? AppHost.GetType().Assembly;
            RootDir = new ResourceVirtualDirectory(this, null, asm);
        }

        public override string CombineVirtualPath(string basePath, string relativePath)
        {
            return string.Concat(basePath, VirtualPathSeparator, relativePath);
        }
    }
}
