using System;
using System.Reflection;
using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public class ResourceVirtualFiles : ResourceVirtualPathProvider
    {
        public ResourceVirtualFiles(Type baseTypeInAssmebly) : base(baseTypeInAssmebly) {}
        public ResourceVirtualFiles(Assembly backingAssembly, string rootNamespace = null) : base(backingAssembly, rootNamespace) {}
    }
}