using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using ServiceStack.CacheAccess;
using ServiceStack.IO;
using ServiceStack.Razor.Compilation;
using ServiceStack.Razor.Managers.RazorGen;

namespace ServiceStack.Razor.Managers
{
    public class CachedRazorViewManager : RazorViewManager
    {
        private readonly ICacheClient cache;

        public CachedRazorViewManager(IRazorConfig viewConfig, IVirtualPathProvider virtualPathProvider, ICacheClient cache) 
            : base(viewConfig, virtualPathProvider)
        {
            this.cache = cache;
        }

        public override RazorPageHost CreatePageHost(IVirtualFile file, RazorViewPageTransformer transformer)
        {
            return new CachedRazorPageHost(PathProvider, file, transformer, new CSharpCodeProvider(), new Dictionary<string, string>(), cache);
        }
    }
}
