using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    public class WebConfigTransformer : AggregateCodeTransformer
    {
        private readonly string DefaultBaseType = typeof(ViewPage).FullName;
        private const string RazorWebPagesSectionName = "system.web.webPages.razor/pages";
        private readonly List<RazorCodeTransformerBase> _transformers = new List<RazorCodeTransformerBase>();

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return _transformers; }
        }

        public override void Initialize(RazorPageHost razorHost, IDictionary<string, string> directives)
        {
            //read the base type here from the web.config here

            EndpointHostConfig.RazorNamespaces
                              .Each(ns => razorHost.NamespaceImports.Add(ns));

            base.Initialize(razorHost, directives);
        }
    }
}