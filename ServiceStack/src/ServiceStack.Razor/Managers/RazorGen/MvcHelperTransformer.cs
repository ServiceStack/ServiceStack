using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Razor.Compilation;
using ServiceStack.Razor.Compilation.CodeTransformers;

namespace ServiceStack.Razor.Managers.RazorGen
{
//    [Export("MvcHelper", typeof(IRazorCodeTransformer))]
    public class MvcHelperTransformer : AggregateCodeTransformer
    {
        private const string WriteToMethodName = "WriteTo";
        private const string WriteLiteralToMethodName = "WriteLiteralTo";
        private readonly RazorCodeTransformerBase[] _transformers = new RazorCodeTransformerBase[] {
            new SetImports(MvcViewTransformer.MvcNamespaces, replaceExisting: false),
            new AddGeneratedClassAttribute(),
            new DirectivesBasedTransformers(),
            new MakeTypeHelper(),
            new RemoveLineHiddenPragmas(),
            new MvcWebConfigTransformer(),
        };

        protected override IEnumerable<RazorCodeTransformerBase> CodeTransformers
        {
            get { return _transformers; }
        }

        public override void Initialize(RazorPageHost razorHost, IDictionary<string, string> directives)
        {
            base.Initialize(razorHost, directives);
            //razorHost.DefaultBaseClass = typeof(System.Web.WebPages.HelperPage).FullName;

            //razorHost.GeneratedClassContext = new GeneratedClassContext(
            //        executeMethodName: GeneratedClassContext.DefaultExecuteMethodName,
            //        writeMethodName: GeneratedClassContext.DefaultWriteMethodName,
            //        writeLiteralMethodName: GeneratedClassContext.DefaultWriteLiteralMethodName,
            //        writeToMethodName: WriteToMethodName,
            //        writeLiteralToMethodName: WriteLiteralToMethodName,
            //        templateTypeName: typeof(System.Web.WebPages.HelperResult).FullName,
            //        defineSectionMethodName: "DefineSection"
            //);
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit,
                                                      CodeNamespace generatedNamespace,
                                                      CodeTypeDeclaration generatedClass,
                                                      CodeMemberMethod executeMethod)
        {

            // Run the base processing
            base.ProcessGeneratedCode(codeCompileUnit, generatedNamespace, generatedClass, executeMethod);

            // Remove the constructor 
            generatedClass.Members.Remove(generatedClass.Members.OfType<CodeConstructor>().SingleOrDefault());
        }
    }
}
