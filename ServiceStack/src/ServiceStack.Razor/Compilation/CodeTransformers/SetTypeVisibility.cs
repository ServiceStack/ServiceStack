using System;
using System.CodeDom;
using System.Reflection;

namespace ServiceStack.Razor.Compilation.CodeTransformers
{
    public class SetTypeVisibility : RazorCodeTransformerBase
    {
        private readonly string _visibility;

        public SetTypeVisibility(string visibility)
        {
            _visibility = visibility;
        }

        public override void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod)
        {
            if (_visibility.Equals("Public", StringComparison.OrdinalIgnoreCase))
            {
                generatedClass.TypeAttributes = generatedClass.TypeAttributes & ~TypeAttributes.VisibilityMask | TypeAttributes.Public;
            }
            else if (_visibility.Equals("Internal", StringComparison.OrdinalIgnoreCase))
            {
                generatedClass.TypeAttributes = generatedClass.TypeAttributes & ~TypeAttributes.VisibilityMask | TypeAttributes.NestedFamANDAssem;
            }
        }
    }
}
