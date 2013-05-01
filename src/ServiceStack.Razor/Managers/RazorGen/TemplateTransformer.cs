using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Razor.Compilation.CodeTransformers;

namespace ServiceStack.Razor.Managers.RazorGen
{
//    [Export("Template", typeof(IRazorCodeTransformer))]
    public class TemplateCodeTransformer : AggregateCodeTransformer
    {
        private const string GenerationEnvironmentPropertyName = "GenerationEnvironment";
        private static readonly IEnumerable<string> _defaultImports = new[] {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text"
        };
        private readonly RazorCodeTransformerBase[] _codeTransforms = new RazorCodeTransformerBase[] {
            new SetImports(_defaultImports, replaceExisting: true),
            new AddGeneratedClassAttribute(),
            new DirectivesBasedTransformers(),
            new SetBaseType("RazorGenerator.Templating.RazorTemplateBase"),
        };

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return _codeTransforms; }
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);
            generatedClass.IsPartial = true;
            // The generated class has a constructor in there by default.
            generatedClass.Members.Remove(generatedClass.Members.OfType<CodeConstructor>().SingleOrDefault());
        }
    }
}
