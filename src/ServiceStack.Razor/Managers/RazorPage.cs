using System;
using System.Web;
using ServiceStack.IO;
using ServiceStack.Razor.Compilation;
using ServiceStack.Text;

namespace ServiceStack.Razor.Managers
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
            return this.PageType.CreateInstance() as RenderingPage;
        }
    }
}