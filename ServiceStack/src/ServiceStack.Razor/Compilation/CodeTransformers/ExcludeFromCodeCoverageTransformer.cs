using System.CodeDom;
using System.Diagnostics.CodeAnalysis;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    public class ExcludeFromCodeCoverageTransformer : RazorCodeTransformerBase
    {
        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            var codeTypeReference = new CodeTypeReference(typeof(ExcludeFromCodeCoverageAttribute));
            generatedClass.CustomAttributes.Add(new CodeAttributeDeclaration(codeTypeReference));
        }
    }
}
