using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor.VirtualPath
{
    public class ResourceVirtualPathProvider : AbstractVirtualPathProviderBase
    {
        #region Fields

        protected ResourceVirtualDirectory rootDir;
        protected Assembly backingAssembly;

        #endregion

        public ResourceVirtualPathProvider(IAppHost appHost, Type typeInBackingAssembly)
            : base(appHost)
        {
            if (typeInBackingAssembly == null)
                throw new ArgumentNullException("typeInBackingAssembly");

            this.backingAssembly = typeInBackingAssembly.Assembly;
            Initialize();
        }

        public ResourceVirtualPathProvider(IAppHost appHost, Assembly backingAssembly)
            : base(appHost)
        {
            if (backingAssembly == null)
                throw new ArgumentNullException("backingAssembly");

            this.backingAssembly = backingAssembly;
            Initialize();
        }

        public ResourceVirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            Initialize();
        }

        protected override sealed void Initialize()
        {
            var asm = backingAssembly ?? AppHost.GetType().Assembly;
            rootDir = new ResourceVirtualDirectory(this, null, asm);
        }

        public override string CombineVirtualPath(string basePath, string relativePath)
        {
            return String.Concat(basePath, VirtualPathSeparator, relativePath);
        }

        #region Properties

        public override IVirtualDirectory RootDirectory { get { return rootDir; } }
        public override string VirtualPathSeparator { get { return "/"; } }
        public override string RealPathSeparator { get { return "."; } }

        #endregion
    }
}
