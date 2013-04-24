using System.CodeDom;
using System.Collections.Generic;

namespace ServiceStack.Razor2.Compilation
{
    public interface IRazorCodeTransformer
    {
        void Initialize(IRazorHost razorHost, IDictionary<string, string> directives);

        void ProcessGeneratedCode(CodeCompileUnit codeCompileUnit, CodeNamespace generatedNamespace, CodeTypeDeclaration generatedClass, CodeMemberMethod executeMethod);

        string ProcessOutput(string codeContent);
    }
}
