namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    internal sealed class RewriteLinePragmas : RazorCodeTransformerBase
    {
        private string _binRelativePath;
        private string _fullPath;

        public override void Initialize(RazorPageHost razorHost, System.Collections.Generic.IDictionary<string, string> directives)
        {
            _binRelativePath = @"..\.." + razorHost.File.VirtualPath;
            _fullPath = razorHost.File.RealPath;
        }

        public override string ProcessOutput(string codeContent)
        {
            return codeContent.Replace("\"" + _fullPath + "\"", "\"" + _binRelativePath + "\"");
        }
    }
}
