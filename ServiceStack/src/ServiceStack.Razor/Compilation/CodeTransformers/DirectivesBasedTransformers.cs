using System;
using System.Collections.Generic;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    public class DirectivesBasedTransformers : AggregateCodeTransformer
    {
        public static readonly string TypeVisibilityKey = "TypeVisibility";
        public static readonly string DisableLinePragmasKey = "DisableLinePragmas";
        public static readonly string TrimLeadingUnderscoresKey = "TrimLeadingUnderscores";
        public static readonly string GenerateAbsolutePathLinePragmas = "GenerateAbsolutePathLinePragmas";
        public static readonly string NamespaceKey = "Namespace";
        public static readonly string ExcludeFromCodeCoverage = "ExcludeFromCodeCoverage";
        public static readonly string SuffixFileName = "ClassSuffix";
        private readonly List<RazorCodeTransformerBase> _transformers = new List<RazorCodeTransformerBase>();

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return _transformers; }
        }

        public override void Initialize(RazorPageHost razorHost, IDictionary<string, string> directives)
        {
            string typeVisibility;
            if (directives.TryGetValue(TypeVisibilityKey, out typeVisibility))
            {
                _transformers.Add(new SetTypeVisibility(typeVisibility));
            }

            string typeNamespace;
            if (directives.TryGetValue(NamespaceKey, out typeNamespace))
            {
                _transformers.Add(new SetTypeNamespace(typeNamespace));
            }

            if (ReadSwitchValue(directives, DisableLinePragmasKey) == true)
            {
                razorHost.EnableLinePragmas = false;
            }
            else if (ReadSwitchValue(directives, GenerateAbsolutePathLinePragmas) != true)
            {
                // Rewrite line pragamas to generate bin relative paths instead of absolute paths.
                _transformers.Add(new RewriteLinePragmas());
            }

            if (ReadSwitchValue(directives, TrimLeadingUnderscoresKey) != false)
            {
                // This should in theory be a different transformer.
                razorHost.DefaultClassName = razorHost.DefaultClassName.TrimStart('_');
            }

            if (ReadSwitchValue(directives, ExcludeFromCodeCoverage) == true)
            {
                _transformers.Add(new ExcludeFromCodeCoverageTransformer());
            }

            string suffix;
            if (directives.TryGetValue(SuffixFileName, out suffix))
            {
                _transformers.Add(new SuffixFileNameTransformer(suffix));
            }

            base.Initialize(razorHost, directives);
        }

        private static bool? ReadSwitchValue(IDictionary<string, string> directives, string key)
        {
            string value;
            bool switchValue;

            if (directives.TryGetValue(key, out value) && Boolean.TryParse(value, out switchValue))
            {
                return switchValue;
            }
            return null;
        }
    }
}
