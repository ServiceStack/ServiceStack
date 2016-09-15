using System;
using System.Reflection;
using ServiceStack.DataAnnotations;
using ServiceStack.IO;

namespace ServiceStack.VirtualPath
{
    public class ResourceVirtualPathProvider : AbstractVirtualPathProviderBase
    {
        protected ResourceVirtualDirectory RootDir;
        protected Assembly BackingAssembly;
        protected string RootNamespace;

        public override IVirtualDirectory RootDirectory => RootDir;
        public override string VirtualPathSeparator => "/";
        public override string RealPathSeparator => ".";

        public ResourceVirtualPathProvider(IAppHost appHost, Type baseTypeInAssmebly)
            : this(appHost, baseTypeInAssmebly.GetAssembly(), GetNamespace(baseTypeInAssmebly)) { }

        public ResourceVirtualPathProvider(IAppHost appHost, Assembly backingAssembly, string rootNamespace=null)
            : base(appHost)
        {
            if (backingAssembly == null)
                throw new ArgumentNullException(nameof(backingAssembly));

            this.BackingAssembly = backingAssembly;
            this.RootNamespace = rootNamespace ?? backingAssembly.GetName().Name;

            Initialize();
        }

        private static string GetNamespace(Type type)
        {
            var attr = type.FirstAttribute<SchemaAttribute>();
            return attr != null ? attr.Name : type.Namespace;
        }

        protected sealed override void Initialize()
        {
            var asm = BackingAssembly ?? AppHost.GetType().GetAssembly();
            RootDir = new ResourceVirtualDirectory(this, null, asm, RootNamespace);
        }

        public override string CombineVirtualPath(string basePath, string relativePath)
        {
            return string.Concat(basePath, VirtualPathSeparator, relativePath);
        }
    }
}
