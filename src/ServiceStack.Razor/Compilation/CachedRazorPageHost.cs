using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.CacheAccess;
using ServiceStack.IO;
using ServiceStack.Logging;

namespace ServiceStack.Razor.Compilation
{
    public class CachedRazorPageHost : RazorPageHost
    {
        public static ILog Log = LogManager.GetLogger(typeof(CachedRazorPageHost));

        private readonly ICacheClient cache;

        public CachedRazorPageHost(IVirtualPathProvider pathProvider,
            IVirtualFile file,
            IRazorCodeTransformer codeTransformer,
            CodeDomProvider codeDomProvider,
            IDictionary<string, string> directives,
            ICacheClient cache)
            : base(pathProvider, file, codeTransformer, codeDomProvider, directives)
        {
            this.cache = cache;
        }

        public override Type Compile()
        {
            var key = "razorcache:" + File.VirtualPath;
            var currentHash = File.GetFileHash();

            var cacheEntry = cache.Get<Entry>(key);
            if (cacheEntry != null)
            {
                if (cacheEntry.Hash == currentHash)
                {
                    var assembly = Assembly.Load(cacheEntry.CompiledAssembly);
                    Log.DebugFormat("Loaded Razor page '{0}' assembly from cache.", File.Name);
                    return assembly.GetTypes().First();
                }
            }

            byte[] assemblyBytes = null;
            var compiledType = CompileAssembly(true, ref assemblyBytes);
            cache.Set(key, new Entry {Hash = currentHash, CompiledAssembly = assemblyBytes});
            return compiledType;
        }

        private class Entry
        {
            public string Hash { get; set; }
            public byte[] CompiledAssembly { get; set; }
        }
    }
}
