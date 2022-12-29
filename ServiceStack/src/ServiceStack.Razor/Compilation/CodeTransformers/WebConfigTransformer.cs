using System.Collections.Generic;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    public class WebConfigTransformer : AggregateCodeTransformer
    {
        private readonly string DefaultBaseType = typeof(ViewPage).FullName;
        private const string RazorWebPagesSectionName = "system.web.webPages.razor/pages";
        private readonly List<RazorCodeTransformerBase> _transformers = new List<RazorCodeTransformerBase>();

        public static HashSet<string> RazorNamespaces { get; set; }

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers => _transformers;

        public override void Initialize(RazorPageHost razorHost, IDictionary<string, string> directives)
        {
            //read the base type here from the web.config here
            foreach (var ns in (RazorNamespaces ?? HostContext.Config.RazorNamespaces))
            {
                razorHost.NamespaceImports.Add(ns);
            }

            base.Initialize(razorHost, directives);
        }
    }
}