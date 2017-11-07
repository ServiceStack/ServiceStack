using System;
using System.Reflection;
using ServiceStack.DataAnnotations;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public class ResourceVirtualFiles 
        : AbstractVirtualPathProviderBase
    {
        protected ResourceVirtualDirectory RootDir;
        protected Assembly BackingAssembly;
        protected string RootNamespace;

        public override IVirtualDirectory RootDirectory => RootDir;
        public override string VirtualPathSeparator => "/";
        public override string RealPathSeparator => ".";
        
        public DateTime LastModified { get; set; } 

        public ResourceVirtualFiles(Type baseTypeInAssmebly)
            : this(baseTypeInAssmebly.Assembly, GetNamespace(baseTypeInAssmebly)) { }

        public ResourceVirtualFiles(Assembly backingAssembly, string rootNamespace=null)
        {
            this.BackingAssembly = backingAssembly ?? throw new ArgumentNullException(nameof(backingAssembly));
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
            var asm = BackingAssembly;
            RootDir = new ResourceVirtualDirectory(this, null, asm, LastModified, RootNamespace);
        }

        public override string CombineVirtualPath(string basePath, string relativePath)
        {
            return string.Concat(basePath, VirtualPathSeparator, relativePath);
        }
    }
}