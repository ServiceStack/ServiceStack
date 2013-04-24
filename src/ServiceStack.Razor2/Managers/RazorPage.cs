using System;
using System.Web;
using ServiceStack.IO;
using ServiceStack.Razor2.Compilation;

namespace ServiceStack.Razor2.Managers
{
    public class RazorPage
    {
        public RazorPage()
        {
            this.IsValid = false;
        }

        public RazorPageHost PageHost { get; set; }
        
        public bool IsValid { get; set; }

        public IVirtualFile File { get; set; }

        public Type PageType { get; set; }
        public Type ModelType { get; set; }

        public virtual HttpCompileException CompileException { get; set; }

        public RenderingPage ActivateInstance()
        {
            return Activator.CreateInstance( this.PageType ) as RenderingPage;
        }
    }
}