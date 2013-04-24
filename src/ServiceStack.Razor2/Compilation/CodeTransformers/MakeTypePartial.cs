using System.CodeDom;

namespace ServiceStack.Razor2.Compilation.CodeTransformers
{
    public class MakeTypePartial : RazorCodeTransformerBase
    {
        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            generatedClass.IsPartial = true;
        }
    }
}
