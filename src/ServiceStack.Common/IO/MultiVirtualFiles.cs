using ServiceStack.VirtualPath;

namespace ServiceStack.IO
{
    public class MultiVirtualFiles : MultiVirtualPathProvider
    {
        public MultiVirtualFiles(params IVirtualPathProvider[] childProviders) : base(childProviders) {}
    }
}