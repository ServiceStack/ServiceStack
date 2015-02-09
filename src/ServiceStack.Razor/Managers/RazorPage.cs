using System;
using System.Web;
using ServiceStack.IO;
using ServiceStack.Razor.Compilation;

namespace ServiceStack.Razor.Managers
{
    public class RazorPage
    {
        private readonly object syncRoot = new object();

        public RazorPage()
        {
            this.IsValid = false;
        }

        public object SyncRoot { get { return syncRoot; } }

        public RazorPageHost PageHost { get; set; }

        public bool MarkedForCompilation { get; set; }
        public bool IsCompiling { get; set; }
        public bool IsValid { get; set; }

        public IVirtualFile File { get; set; }
        public string VirtualPath { get; set; }

        public Type PageType { get; set; }
        public Type ModelType { get; set; }

        public virtual HttpCompileException CompileException { get; set; }

        public RenderingPage ActivateInstance()
        {
            return this.PageType.CreateInstance() as RenderingPage;
        }
    }
}