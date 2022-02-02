using System.Collections.Generic;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    public class SetTypeNamespace : RazorCodeTransformerBase
    {
        private readonly string _namespace;

        public SetTypeNamespace(string @namespace)
        {
            _namespace = @namespace;
        }

        public override void Initialize(RazorPageHost razorHost, IDictionary<string, string> directives)
        {
            razorHost.DefaultNamespace = _namespace;
        }
    }
}
